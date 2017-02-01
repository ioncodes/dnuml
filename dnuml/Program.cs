using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using dnlib.DotNet;

namespace dnuml
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ModuleDefMD module = ModuleDefMD.Load(args[0]);
            YumlMe yme = new YumlMe();
            foreach (var type in module.Types)
            {
                Access access = Access.None;
                if(type.IsPublic)
                    access = Access.Public;
                else if(type.IsNotPublic)
                    access = Access.Private;
                YumlClass yc = new YumlClass(type.Name, access);
                foreach (var method in type.Methods)
                {
                    access = Access.None;
                    if(type.IsPublic)
                        access = Access.Public;
                    else if(type.IsNotPublic)
                        access = Access.Private;

                    List<string> parameters = method.Parameters.Select(param => param.Name).ToList();
                    string mname = method.Name;
                    if (mname.StartsWith("."))
                    {
                        mname = mname.Remove(0, 1);
                    }
                    mname = mname.Replace("_", "");
                    YumlMethod ym = new YumlMethod(mname, parameters, access);
                    yc.AddMethod(ym);
                }
                yme.AddClass(yc);
            }
            byte[] bImage = yme.Build();
            File.WriteAllBytes("image.png", bImage);

            Console.Read();
        }
    }
}