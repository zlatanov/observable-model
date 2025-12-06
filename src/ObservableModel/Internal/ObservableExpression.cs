using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace ObservableModel
{
    internal sealed class ObservableExpression<T, TValue> : ExpressionVisitor, IObservable<TValue>
        where T : INotifyPropertyChanged
    {
        private sealed class Binding
        {
            public Binding( Binding? parent )
            {
                m_parent = parent;
            }


            public Action? Changed;


            public Dictionary<string, Binding> Bindings { get; } = new Dictionary<string, Binding>( StringComparer.Ordinal );


            public void Update( object? source )
            {
                if ( m_source != source )
                {
                    if ( m_source != null )
                    {
                        if ( m_source is INotifyPropertyChanged notifiable )
                        {
                            notifiable.PropertyChanged -= OnSourcePropertyChanged;
                        }

                        m_source = null;
                    }

                    try
                    {
                        m_pendingRaise = true;

                        if ( source != null )
                        {
                            m_source = source;

                            if ( source is INotifyPropertyChanged notifiable )
                            {
                                notifiable.PropertyChanged += OnSourcePropertyChanged;
                            }

                            foreach ( var binding in Bindings )
                            {
                                binding.Value.Update( GetValue( binding.Key ) );
                            }
                        }
                    }
                    finally
                    {
                        m_pendingRaise = false;
                    }

                    RaiseChanged();
                }
            }


            public object? GetValue( string propertyName )
            {
                if ( m_source != null )
                {
                    if ( !m_bindingProperties.TryGetValue( propertyName, out var property ) )
                    {
                        var declaringType = m_source.GetType();
                        property = ReflectionHelper.GetProperty( declaringType, propertyName );

                        if ( property is null )
                            throw new InvalidOperationException( $"Property {propertyName} was not found in {declaringType}." );

                        m_bindingProperties.Add( propertyName, property );
                    }

                    return property.GetValue( m_source );
                }

                return null;
            }


            public override string ToString() => String.Join( ", ", Bindings.Keys );


            private void OnSourcePropertyChanged( object? sender, PropertyChangedEventArgs e )
            {
                if ( String.IsNullOrEmpty( e.PropertyName ) )
                {
                    foreach ( var binding in Bindings )
                    {
                        binding.Value.Update( GetValue( binding.Key ) );
                    }
                }
                else if ( Bindings.TryGetValue( e.PropertyName, out var binding ) )
                {
                    binding.Update( GetValue( e.PropertyName ) );
                }
            }


            private void RaiseChanged()
            {
                if ( !m_pendingRaise )
                {
                    Changed?.Invoke();
                    m_parent?.RaiseChanged();
                }
            }


            private object? m_source;
            private bool m_pendingRaise;
            private readonly Binding? m_parent;
            private readonly Dictionary<string, PropertyInfo> m_bindingProperties = [];
        }


        private sealed class Subscriber : IDisposable
        {
            public Subscriber( ObservableExpression<T, TValue> target, IObserver<TValue> observer )
            {
                m_target = target;
                m_observer = observer;
            }


            public void Dispose()
            {
                var observer = Interlocked.Exchange( ref m_observer, null );

                if ( observer is object )
                {
                    m_target!.Unsubscribe( this );
                    m_target = null;
                }
            }


            internal void OnNext( TValue value ) => m_observer?.OnNext( value );


            private ObservableExpression<T, TValue>? m_target;
            private IObserver<TValue>? m_observer;
        }


        public ObservableExpression( T source, Expression<Func<T, TValue>> expression )
        {
            m_source = source;
            m_expression = ObservableExpressionCache.GetOrCreate( expression );
            m_currentValue = default!;

            m_binding = new Binding( null )
            {
                Changed = OnBindingChanged
            };

            Visit( expression );
        }


        public IDisposable Subscribe( IObserver<TValue> observer )
        {
            var subscriber = new Subscriber( this, observer ?? throw new ArgumentNullException( nameof( observer ) ) );

            lock ( m_subscribers )
            {
                // When we are notifying we do not want to insert the new subscriber somewhere in the middle, because
                // it might cause a double notification.
                if ( !m_notifying && m_subscribers.Count > m_subscribersActiveCount )
                {
                    // Find an empty spot for the new subscriber
                    for ( var i = 0; i < m_subscribers.Count; ++i )
                    {
                        if ( m_subscribers[ i ] is null )
                        {
                            m_subscribers[ i ] = subscriber;
                            break;
                        }
                    }
                }
                else
                {
                    m_subscribers.Add( subscriber );
                }

                var value = m_currentValue;

                if ( ++m_subscribersActiveCount == 1 )
                {
                    m_binding.Update( m_source );
                }

                // If the value hasn't changed, we need to publish it to the observer. 
                if ( EqualityComparer<TValue>.Default.Equals( value, m_currentValue ) )
                {
                    observer.OnNext( value );
                }
            }

            return subscriber;
        }


        private void Unsubscribe( Subscriber subscriber )
        {
            lock ( m_subscribers )
            {
                for ( var i = 0; i < m_subscribers.Count; ++i )
                {
                    if ( m_subscribers[ i ] == subscriber )
                    {
                        m_subscribers[ i ] = null;
                        m_subscribersActiveCount -= 1;

                        break;
                    }
                }
            }
        }


        protected override Expression VisitMember( MemberExpression node )
        {
            var supported = false;
            var parentNode = node.Expression;

            while ( node != parentNode && parentNode is not null )
            {
                if ( parentNode.NodeType == ExpressionType.Parameter && parentNode.Type == typeof( T ) )
                {
                    supported = true;
                }
                else
                {
                    if ( parentNode.NodeType == ExpressionType.MemberAccess )
                    {
                        var memberExpression = (MemberExpression)parentNode;

                        if ( memberExpression.Member is PropertyInfo && typeof( INotifyPropertyChanged ).IsAssignableFrom( memberExpression.Member.DeclaringType ) )
                        {
                            parentNode = memberExpression.Expression;

                            continue;
                        }
                    }
                }

                break;

            }

            // Only nested properties are supported at the moment
            if ( supported )
            {
                var parentBinding = m_binding;

                foreach ( var propertyName in node.ToString().Split( '.' ).Skip( 1 ) )
                {
                    if ( !parentBinding.Bindings.TryGetValue( propertyName, out var currentBinding ) )
                    {
                        currentBinding = new Binding( parentBinding );
                        parentBinding.Bindings.Add( propertyName, currentBinding );
                    }

                    parentBinding = currentBinding;
                }
            }

            return base.VisitMember( node );
        }


        private void OnBindingChanged()
        {
            lock ( m_subscribers )
            {
                var nextValue = m_expression( m_source );

                if ( !EqualityComparer<TValue>.Default.Equals( m_currentValue, nextValue ) )
                {
                    m_currentValue = nextValue;
                    m_notifying = true;

                    try
                    {
                        var count = m_subscribers.Count;

                        for ( var i = 0; i < count; i++ )
                        {
                            // The subscriber might be null if unsubscribed
                            m_subscribers[ i ]?.OnNext( nextValue );
                        }
                    }
                    finally
                    {
                        m_notifying = false;
                    }
                }
            }
        }


        private readonly Func<T, TValue> m_expression;
        private readonly T m_source;
        private readonly Binding m_binding;

        private readonly List<Subscriber?> m_subscribers = [];
        private int m_subscribersActiveCount;
        private bool m_notifying;

        private TValue m_currentValue;
    }
}
