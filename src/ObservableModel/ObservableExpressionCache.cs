using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ObservableModel
{
    public static class ObservableExpressionCache
    {
        private static readonly Dictionary<Key, Delegate> Cache = [];
        private static readonly Dictionary<string, ExpressionAndDelegate> StringCache = new( StringComparer.Ordinal );


        public static TDelegate GetOrCreate<TDelegate>( string key, Func<Expression<TDelegate>> expression ) where TDelegate : Delegate
        {
            lock ( StringCache )
            {
                if ( !StringCache.TryGetValue( key, out var value ) )
                {
                    value = new ExpressionAndDelegate
                    {
                        Expression = expression()
                    };
                    value.Delegate = ( (Expression<TDelegate>)value.Expression ).Compile();

                    StringCache.Add( key, value );
                }

                return (TDelegate)value.Delegate;
            }
        }


        public static TDelegate GetOrCreate<TDelegate>( string key, Func<Expression<TDelegate>> expression, out Expression<TDelegate> cachedExpression ) where TDelegate : Delegate
        {
            lock ( StringCache )
            {
                if ( !StringCache.TryGetValue( key, out var value ) )
                {
                    value = new ExpressionAndDelegate
                    {
                        Expression = expression()
                    };
                    value.Delegate = ( (Expression<TDelegate>)value.Expression ).Compile();

                    StringCache.Add( key, value );
                }

                cachedExpression = (Expression<TDelegate>)value.Expression;

                return (TDelegate)value.Delegate;
            }
        }


        public static TDelegate GetOrCreate<TDelegate>( Expression<TDelegate> expression ) where TDelegate : Delegate
        {
            var key = new Key( expression );

            lock ( Cache )
            {
                if ( !Cache.TryGetValue( key, out var @delegate ) )
                {
                    key.PrepareForCache();

                    Cache.Add( key, @delegate = expression.Compile() );
                }

                return (TDelegate)@delegate;
            }
        }


        private sealed class Key : ExpressionVisitor
        {
            private static readonly object True = new();
            private static readonly object False = new();


            public Key( Expression expression )
            {
                m_expression = expression;

                Visit( expression );
            }


            public void PrepareForCache()
            {
                m_cached = true;

                Visit( m_expression );
            }


            public override Expression? Visit( Expression? node )
            {
                if ( !Test( node?.Type ) ) return null;

                return base.Visit( node );
            }


            protected override Expression VisitMember( MemberExpression node )
            {
                if ( !Test( node.Member ) ) return node;

                return base.VisitMember( node );
            }


            protected override Expression VisitBinary( BinaryExpression node )
            {
                if ( !Test( node.Method ) ) return node;
                if ( !Test( node.IsLifted ? True : False ) ) return node;
                if ( !Test( node.IsLiftedToNull ? True : False ) ) return node;

                return base.VisitBinary( node );
            }


            protected override Expression VisitConstant( ConstantExpression node )
            {
                if ( !Test( node.Value ) ) return node;

                return base.VisitConstant( node );
            }


            protected override Expression VisitMethodCall( MethodCallExpression node )
            {
                if ( !Test( node.Method ) ) return node;

                return base.VisitMethodCall( node );
            }


            protected override Expression VisitIndex( IndexExpression node )
            {
                if ( !Test( node.Indexer ) ) return node;

                return base.VisitIndex( node );
            }


            public override int GetHashCode() => m_hashCode.ToHashCode();


            public override bool Equals( object? obj )
            {
                var equal = false;

                if ( obj is Key other )
                {
                    m_compareIndex = 0;

                    Visit( other.m_expression );

                    equal = m_compareIndex != Int32.MaxValue;
                    m_compareIndex = -1;
                }

                return equal;
            }


            private bool Test( object? value )
            {
                if ( m_compareIndex == -1 )
                {
                    if ( m_cached )
                    {
                        m_compareObjects.Add( value );
                    }
                    else
                    {
                        m_hashCode.Add( value );
                    }

                    return true;
                }

                if ( m_compareObjects.Count > m_compareIndex && Equals( m_compareObjects[ m_compareIndex ], value ) )
                {
                    ++m_compareIndex;

                    return true;
                }

                m_compareIndex = Int32.MaxValue;

                return false;
            }


            private readonly Expression m_expression;
            private readonly List<object?> m_compareObjects = [];
            private int m_compareIndex = -1;

            private bool m_cached;
            private HashCode m_hashCode;
        }


        private struct ExpressionAndDelegate
        {
            public Expression Expression;
            public Delegate Delegate;
        }
    }
}
