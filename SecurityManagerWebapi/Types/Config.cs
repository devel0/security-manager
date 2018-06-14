using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SearchAThing.NETCoreUtil;

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

        public void SaveCred(CredInfo cred, string credorigname)
        {
            lock (lck)
            {
                if (string.IsNullOrEmpty(credorigname))
                {
                    // trim spaces
                    cred.Name = cred.Name?.Trim();
                    cred.Email = cred.Email?.Trim();
                    cred.Password = cred.Password?.Trim();

                    Credentials.Add(cred);
                }
                else
                {
                    var q = Credentials.FirstOrDefault(w => w.Name.Equals(credorigname.Trim(), StringComparison.CurrentCultureIgnoreCase));
                    if (q == null) throw new Exception($"unable to find [{credorigname}] entry");

                    q.Name = cred.Name?.Trim();
                    q.Email = cred.Email?.Trim();
                    q.Url = cred.Url?.Trim();
                    q.Username = cred.Username?.Trim();
                    q.Email = cred.Email?.Trim();
                    q.Password = cred.Password?.Trim();
                    q.Notes = cred.Notes;
                }
            }
            Save();
        }

        public CredInfo LoadCred(string name)
        {
            CredInfo nfo;
            lock (lck)
            {
                nfo = Credentials.FirstOrDefault(w => w.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));
            }

            return nfo;
        }

        public void DeleteCred(string name)
        {
            CredInfo nfo;
            lock (lck)
            {
                nfo = Credentials.FirstOrDefault(w => w.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));
                if (nfo != null) Credentials.Remove(nfo);
            }
        }        

        public List<CredShort> GetCredShortList(string filter)
        {
            List<CredShort> res;

            if (!string.IsNullOrEmpty(filter)) filter = filter.Trim();

            lock (lck)
            {
                res = Credentials.Where(r => new[] { r.Name, r.Url, r.Username, r.Email, r.Notes }.MatchesFilter(filter))
                .Select(w => new CredShort() { Name = w.Name, Url = w.Url })
                .ToList();
            }

            return res;
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