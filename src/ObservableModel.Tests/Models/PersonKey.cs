namespace ObservableModel
{
    public readonly struct PersonKey
    {
        public PersonKey( Person person, string phone )
        {
            Person = person;
            Phone = phone;
        }


        public Person Person { get; }


        public string Phone { get; }
    }
}
