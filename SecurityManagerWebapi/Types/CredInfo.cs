using System;

namespace SecurityManagerWebapi
{

    public class CredInfo
    {

        /// <summary>
        /// displayed as Service
        /// </summary>
        public string Name { get; set; }
        
        public string Url { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Notes { get; set; }

        /// <summary>
        /// null for new entries
        /// </summary>        
        public string GUID { get; set; }

    }

}