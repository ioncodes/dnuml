using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnuml
{
    static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ModuleDefMD module = ModuleDefMD.Load(args[0]);
            var refs = GetReferences(module);
            YumlMe yme = new YumlMe();
            foreach (var type in module.Types)
            {
                Access access = Access.None;
                if(type.IsPublic)
                    access = Access.Public;
                else if(type.IsNotPublic)
                    access = Access.Private;
                string tn = type.Name;
                tn = tn.Sanitize();
                YumlClass yc = new YumlClass(tn, access);
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
                    mname = mname.Sanitize();
                    YumlMethod ym = new YumlMethod(mname, parameters, access);
                    yc.AddMethod(ym);
                }
                foreach (var @ref in refs)
                {
                    if (type.Name == @ref[0])
                    {
                        string name = @ref[1];
                        name = name.Sanitize();
                        yc.References.Add(new YumlClass(name, Access.None));
                    }
                }
                if (type.HasFields)
                {
                    foreach (var field in type.Fields)
                    {
                        access = Access.None;
                        if (field.IsPublic)
                            access = Access.Public;
                        else if (field.IsPrivate)
                            access = Access.Private;
                        string name = field.Name;
                        name = name.Sanitize();
                        yc.Variables.Add(new YumlVariable(name, access));
                    }
                }
                if (type.HasProperties)
                {
                    foreach (var property in type.Properties)
                    {
                        string name = property.Name;
                        name = name.Sanitize();
                        yc.Variables.Add(new YumlVariable(name, Access.None));
                    }
                }
                yme.AddClass(yc);
            }
            byte[] bImage = yme.Build().Result;
            SaveFileDialog sfd = new SaveFileDialog
            {
                RestoreDirectory = true,
                Filter = "JPEG Image|*.jpg|PNG Image|*.png",
                Title = "Where do you want your UML?"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(sfd.FileName, bImage);
                Console.WriteLine("Saved!");
            }
            else
            {
                Console.WriteLine("Error saving file!");
            }
        }

        private static List<string[]> GetReferences(ModuleDef module)
        {
            var typeSet = new HashSet<string>();
            foreach (var type in module.Types)
            {
                typeSet.Add(type.Name);
            }
            var refs = (from type in module.Types from method in type.Methods where method.HasBody from instruction in method.Body.Instructions where instruction.OpCode == OpCodes.Call from set in typeSet where type.Name != set where instruction.Operand.ToString().Contains(set) select new string[] {type.Name, set}).ToList();
            return refs.GroupBy(strArr => string.Join("|", strArr))
                .Select(g => g.First())
                .ToList();
        }

        private static string Sanitize(this string s)
        {
            //return s.Replace("_", "").Replace("<", "").Replace(">", "");
            return s;
        }
    }
}