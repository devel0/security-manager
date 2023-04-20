namespace SecurityManagerWebapi.Controllers
{
    [Route("[controller]/[action]")]
    public class ApiController : Controller
    {

        Global global { get { return Global.Instance; } }
        Config config { get { return global.Config; } }

        #region constructor
        public ApiController()
        {
        }
        #endregion

        #region helpers
        CommonResponse InvalidAuthResponse()
        {
            return new CommonResponse() { ExitCode = CommonResponseExitCodes.InvalidAuth };
        }

        CommonResponse SuccessfulResponse()
        {
            return new CommonResponse() { ExitCode = CommonResponseExitCodes.Successful };
        }

        CommonResponse ErrorResponse(string errMsg)
        {
            return new CommonResponse()
            {
                ExitCode = CommonResponseExitCodes.Error,
                ErrorMsg = errMsg
            };
        }

        int CurrentLevel(string password, int pin)
        {
            return config.Credentials.Where(w => w.Name == "Security Manager" && w.Password == password && w.Pin == pin).OrderByDescending(w => w.Level).First().Level;
        }

        bool CheckAuth(string password, int pin)
        {
            var inCredentials = config.Credentials.FirstOrDefault(w => w.Name == "Security Manager" && w.Password == password && w.Pin == pin);

            var is_valid = !string.IsNullOrEmpty(config?.AdminPassword) &&
                            config.Pin != 0 &&
                            inCredentials != null ||
                            (inCredentials == null && config?.AdminPassword == password && config.Pin == pin);

            if (!is_valid)
            {
                var q = HttpContext.Request.Headers["X-Real-IP"];
                var url = "";
                if (q.Count > 0) url = q.First();
                global.LogWarning($"invalid login attempt from [{url}]");
                // todo : autoban
            }

            return is_valid;
        }
        #endregion

        [HttpPost]
        public CommonResponse SaveCred(string password, int pin, CredInfo cred)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();
                
                var curLvl = CurrentLevel(password, pin);
                var dbCredLvl = curLvl;
                var q0 = config.Credentials.FirstOrDefault(w => w.GUID == cred.GUID);                                
                if (q0 != null) dbCredLvl = q0.Level;

                var canEditLevel =
                    curLvl == 99 || // superuser
                    (string.IsNullOrEmpty(cred.GUID) && cred.Level >= curLvl) || // can't create lower level sec credentials
                    (!string.IsNullOrEmpty(cred.GUID) && cred.Level == dbCredLvl); // can't change existing item level

                if (!canEditLevel) return InvalidAuthResponse();

                config.SaveCred(cred);

                return SuccessfulResponse();
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse RandomPassword(string password, int pin, int length)
        {
            try
            {
                if (length == 0) length = 8;

                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var res = new RandomPasswordResponse();

                res.Password = SearchAThing.Ext.Toolkit.RandomPassword(new RandomPasswordOptions
                {
                    Length = length,
                    AvoidChars = new[] { 'I', 'l', '0', 'O' }
                });

                return res;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse RandomPin(string password, int pin, int length)
        {
            try
            {
                if (length == 0) length = 4;

                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var res = new RandomPasswordResponse();

                res.Password = SearchAThing.Ext.Toolkit.RandomPassword(new RandomPasswordOptions
                {
                    Length = length,
                    AtLeastOneUppercase = false,
                    AllowLetter = false
                });

                return res;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }


        [HttpPost]
        public CommonResponse LoadCred(string password, int pin, string guid)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var response = new CredInfoResponse();

                response.Cred = config.LoadCred(guid);
                if (response.Cred.Level > CurrentLevel(password, pin)) return InvalidAuthResponse(); // can't access higher level of credentials

                return response;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse DeleteCred(string password, int pin, string guid)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var q = config.LoadCred(guid);
                if (q.Level > CurrentLevel(password, pin)) return InvalidAuthResponse(); // can't access higher level of credentials

                config.DeleteCred(guid);

                return SuccessfulResponse();
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse CredShortList(string password, int pin, string filter)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var response = new CredShortListResponse();

                response.CredShortList = config.GetCredShortList(filter, CurrentLevel(password, pin));

                return response;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse Aliases(string password, int pin)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();

                var response = new AliasResponse();

                response.Aliases = config.GetAliases(CurrentLevel(password, pin)).ToList();

                return response;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

        [HttpPost]
        public CommonResponse IsAuthValid(string password, int pin)
        {
            try
            {
                if (!CheckAuth(password, pin)) return InvalidAuthResponse();
                var res = new AuthResponse();
                res.currentLevel = CurrentLevel(password, pin);
                return res;
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message);
            }
        }

    }
}
