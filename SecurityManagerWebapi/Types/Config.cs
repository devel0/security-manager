using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SearchAThing;

namespace SecurityManagerWebapi
{

    public class Config
    {

        Global global { get { return Global.Instance; } }

        object lck = new object();

        public Config()
        {
            if (Credentials == null) Credentials = new List<CredInfo>();
        }

        public void SaveCred(CredInfo cred)
        {
            lock (lck)
            {
                if (string.IsNullOrEmpty(cred.GUID))
                {
                    cred.GUID = Guid.NewGuid().ToString();

                    // trim spaces
                    cred.Name = cred.Name?.Trim();
                    cred.Email = cred.Email?.Trim();
                    cred.Password = cred.Password?.Trim();
                    cred.Pin = cred.Pin;
                    cred.Level = cred.Level;
                    cred.PasswordRegenLength = cred.PasswordRegenLength;
                    cred.CreateTimestamp = DateTime.UtcNow;

                    Credentials.Add(cred);
                }
                else
                {
                    var q = Credentials.FirstOrDefault(w => w.GUID == cred.GUID);
                    if (q == null) throw new Exception($"unable to find [{cred.GUID}] entry");

                    q.Name = cred.Name?.Trim();
                    q.Email = cred.Email?.Trim();
                    q.Url = cred.Url?.Trim();
                    q.Username = cred.Username?.Trim();
                    q.Email = cred.Email?.Trim();
                    q.Password = cred.Password?.Trim();
                    q.Pin = cred.Pin;
                    q.Level = cred.Level;
                    q.PasswordRegenLength = cred.PasswordRegenLength;
                    q.Notes = cred.Notes;
                    q.ModifyTimestamp = DateTime.UtcNow;
                }
            }
            Save();
        }

        public CredInfo LoadCred(string guid)
        {
            CredInfo nfo;
            lock (lck)
            {
                nfo = Credentials.FirstOrDefault(w => w.GUID == guid);
            }

            return nfo;
        }

        public void DeleteCred(string guid)
        {
            CredInfo nfo;
            lock (lck)
            {
                nfo = Credentials.FirstOrDefault(w => w.GUID == guid);
                if (nfo != null) Credentials.Remove(nfo);
            }
        }

        public List<CredShort> GetCredShortList(string filter, int lvl)
        {
            List<CredShort> res;

            if (!string.IsNullOrEmpty(filter)) filter = filter.Trim();

            lock (lck)
            {
                res = Credentials.Where(r => new[] { r.Name, r.Url, r.Username, r.Email, r.Notes }.MatchesFilter(filter) && r.Level <= lvl)
                .Select(w => new CredShort() { GUID = w.GUID, Name = w.Name, Username = w.Username, Email = w.Email, Url = w.Url, Level = w.Level })
                .ToList();
            }

            return res;
        }

        public IEnumerable<Alias> GetAliases(int curLvl)
        {
            lock (lck)
            {
                var hsNames = new HashSet<string>();
                var hsUsernames = new HashSet<string>();
                var hsEmails = new HashSet<string>();
                foreach (var x in Credentials.Where(r => r.Level <= curLvl))
                {
                    hsNames.Add(x.Name);
                    hsUsernames.Add(x.Username);
                    hsEmails.Add(x.Email);
                }

                var nEn = hsNames.GetEnumerator();
                var uEn = hsUsernames.GetEnumerator();
                var eEn = hsEmails.GetEnumerator();

                while (true)
                {
                    var nAvail = nEn.MoveNext();
                    var uAvail = uEn.MoveNext();
                    var eAvail = eEn.MoveNext();
                    if (!nAvail && !uAvail && !eAvail) break;
                    yield return new Alias()
                    {
                        Name = nAvail ? nEn.Current : null,
                        Username = uAvail ? uEn.Current : null,
                        Email = eAvail ? eEn.Current : null
                    };
                }
            }
        }

        public void Save()
        {
            lock (lck)
            {
                try
                {
                    if (File.Exists(Global.AppConfigPathfilename))
                        File.Copy(Global.AppConfigPathfilename, Global.AppConfigPathfilenameBackup, true);
                }
                catch (Exception ex)
                {
                    global.LogError($"unable to backup config file [{Global.AppConfigPathfilename}] to [{Global.AppConfigPathfilenameBackup}] : {ex.Message}");
                }
                File.WriteAllText(Global.AppConfigPathfilename, JsonConvert.SerializeObject(this, Formatting.Indented));
                // save with mode 600
                LinuxHelper.SetFilePermission(Global.AppConfigPathfilename, 384);
            }
        }

        public string AdminPassword { get; set; }
        public int Pin { get; set; }

        public List<CredInfo> Credentials { get; set; }

    }

}