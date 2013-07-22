namespace iDeal.Directory
{
    public class Issuer
    {
        public string Name { get; private set; }

        /// <summary>
        /// Issuers are listed in the shortlist or the longlist
        /// </summary>
        public string Code { get; private set; }

        public Issuer(string code, string name)
        {
            Code = code;
            Name = name;
        }
    }
}
