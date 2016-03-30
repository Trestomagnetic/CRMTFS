namespace SkyTfs
{
    public class TfsHyperlink
    {
        private string _comment;
        public string Comment { get { return _comment; } }

        private string _location;
        public string Location { get { return _location; } }

        public TfsHyperlink(string comment, string location)
        {
            _comment = comment;
            _location = location;
        }
    }
}
