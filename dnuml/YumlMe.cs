using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace dnuml
{
    public class YumlMe
    {
        private const string _url = "https://yuml.me/diagram/plain/class/";
        private const string _baseUrl = "https://yuml.me/";
        private const char _classPrefix = '[';
        private const char _classSuffix = ']';
        private const char _delimiter = ';';
        private const char _sectionDivider = '|';
        private const string _arrow = "->";

        public List<YumlClass> Classes { get; } = new List<YumlClass>();
        public string QueryString { get; private set; }

        public void AddClass(YumlClass @class)
        {
            Classes.Add(@class);
        }

        public async Task<byte[]> Build()
        {
            for (var c = 0; c < Classes.Count; c++)
            {
                var yclass = Classes[c];
                QueryString += _classPrefix + yclass.Name;
                // variables
                if (yclass.Variables.Count > 0)
                    QueryString += _sectionDivider;
                for (var index = 0; index < yclass.Variables.Count; index++)
                {
                    var yvar = yclass.Variables[index];
                    QueryString += yvar.Access.ToAccess() + yvar.Name;
                    if (index != yclass.Variables.Count - 1)
                        QueryString += _delimiter;
                }

                // methods
                if (yclass.Methods.Count > 0)
                    QueryString += _sectionDivider;
                for (var index = 0; index < yclass.Methods.Count; index++)
                {
                    var ymethod = yclass.Methods[index];
                    QueryString += ymethod.Access.ToAccess() + ymethod.Name + '(';
                    for (var i = 0; i < ymethod.Parameters.Count; i++)
                    {
                        var yarg = ymethod.Parameters[i];
                        string arg = yarg;
                        if (arg == "")
                            arg = "random" + index;
                        QueryString += arg;
                        if (i != ymethod.Parameters.Count - 1)
                            QueryString += '/';
                    }
                    QueryString += ')';
                    if (index != yclass.Methods.Count - 1)
                        QueryString += _delimiter;
                }
                QueryString += _classSuffix;
                if(c != Classes.Count -1)
                    QueryString += ',';
            }
            foreach (var c in Classes)
            {
                foreach (var r in c.References)
                {
                    QueryString += ",[" + c.Name + "]" + _arrow + "[" + r.Name + "]";
                }
            }
            var commandContentBytes = System.Text.Encoding.UTF8.GetBytes(QueryString);
            var invalidPathChars = System.IO.Path.GetInvalidPathChars().Select(x=>Convert.ToByte(x));

            var found = commandContentBytes.Intersect(invalidPathChars).ToArray();
            // Console.WriteLine(System.Text.Encoding.UTF8.GetString(found));
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                {"dsl_text", QueryString},
            };
            var items = values.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
            var content = new StringContent(String.Join("&", items), null, "application/x-www-form-urlencoded");
            var response = await client.PostAsync(_url, content);
            var id = await response.Content.ReadAsStringAsync();
            client = new HttpClient();
            response = await client.GetAsync(_baseUrl + id);
            return await response.Content.ReadAsByteArrayAsync();
        }
    }

    public class YumlClass
    {
        public YumlClass(string name, Access access)
        {
            Name = name;
            Access = access;
            Methods = new List<YumlMethod>();
            Variables = new List<YumlVariable>();
            References = new List<YumlClass>();
        }

        public void AddVariable(YumlVariable variable)
        {
            Variables.Add(variable);
        }

        public void AddMethod(YumlMethod method)
        {
            Methods.Add(method);
        }

        public void AddReference(YumlClass @class)
        {
            References.Add(@class);
        }

        public string Name { get; }
        public Access Access { get; }
        public List<YumlMethod> Methods { get; }
        public List<YumlVariable> Variables { get; }
        public List<YumlClass> References { get; }
    }

    public class YumlVariable
    {
        public YumlVariable(string name, Access access)
        {
            Name = name;
            Access = access;
        }

        public string Name { get; }
        public Access Access { get; }
    }

    public class YumlMethod
    {
        public YumlMethod(string name, List<string> parameters, Access access)
        {
            Name = name;
            Parameters = parameters;
            Access = access;
        }

        public string Name { get; }
        public List<string> Parameters { get; }
        public Access Access { get; }
    }

    public enum Access
    {
        Public,
        Private,
        None
    }

    internal static class Helpers
    {
        public static char ToAccess(this Access access)
        {
            switch (access)
            {
                case Access.Private:
                    return '-';
                case Access.Public:
                    return '+';
                case Access.None:
                    return ' ';
            }
            return ' ';
        }
    }
}