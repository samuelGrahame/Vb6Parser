using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Vb6Parser
{
    public class FrmParser
    {
        private string Name;
        private string Directory;

        public FrmParser(string directory, string name)
        {
            Directory = directory;
            Name = name;
        }

        public List<KeyValuePair<string, string>> NameTypePairs = new List<KeyValuePair<string, string>>();
        public Dictionary<string, int> NameCount = new Dictionary<string, int>();

        public string FormString;

        public void WritePropertyFields(StringBuilder builder, string field, ref int index, ref string[] tokens, string indent)
        {
            var startindent = indent;
            var fieldWith = field;
            Stack<string> stack = new Stack<string>();
            stack.Push(field);

            for (; index < tokens.Length; index++)
            {
                var token = tokens[index];
                if(token == "Begin")
                {
                    index++;
                    var type = tokens[index];
                    index++;
                    var name = tokens[index];
                    index++;

                    int indexOfMe = 0;
                    bool hasIndex = false;
                    if(NameCount.ContainsKey(name))
                    {
                        indexOfMe = NameCount[name];
                        hasIndex = true;
                        NameCount[name]++;
                    }
                    else
                    {
                        indexOfMe = 0;

                        for (int i = index; i < tokens.Length; i++)
                        {
                            if(tokens[i] == "Begin" && tokens[i + 1] == type && tokens[i + 2] == name)
                            {
                                NameCount[name] = 1;
                                hasIndex = true;
                                break;
                            }
                        }
                    }

                    if(!hasIndex)
                    {
                        NameTypePairs.Add(new KeyValuePair<string, string>(name, type));
                    }                    

                    builder.AppendLine($"{indent}{field}.Controls.Add({(hasIndex ? $"{name}[{indexOfMe}]" : name)} = new {type}() {{");

                    WritePropertyFields(builder, "", ref index, ref tokens, startindent + "\t");

                    builder.AppendLine($"{indent}}});");
                }
                else if(token == "End")
                {
                    return;
                }else if(token == "BeginProperty")
                {
                    index++;
                    if(tokens[index] == "ListImage1")
                    {
                        tokens[index] = "ListImage1";
                    }
                    builder.AppendLine($"{indent}{fieldWith}.{tokens[index]} = new object() {{");

                    fieldWith = "";

                    indent += "\t";

                    stack.Push("");                    
                    

                    if(tokens[index].StartsWith("{") && tokens[index].EndsWith("}"))
                    {
                        index++;                        
                    }
                }
                else if(token == "EndProperty")
                {
                    // TODO
                    indent = startindent;
                    stack.Pop();
                    fieldWith = stack.Peek();

                    builder.AppendLine($"{indent}}}");
                }
                else if(token == "\r")
                {

                }else if(token.StartsWith("{") && token.EndsWith("}"))
                {

                }
                else
                {
                    builder.Append($"{indent}{fieldWith}.{token}");
                    index++;
                    token = tokens[index];
                    if(token == "=")
                    {
                        builder.Append(" = ");

                        index++;
                        token = tokens[index];

                        var ending = string.IsNullOrWhiteSpace(fieldWith) ? "," : ";";

                        if (token.StartsWith("\""))
                        {
                            builder.AppendLine($"{token}{ending}");
                            // start string?
                            if (tokens[index + 1] == ":")
                            {
                                index++;
                                // resource?
                            }
                        }
                        else
                        {
                            builder.AppendLine($"{token}{ending}");
                        }

                        for (; index < tokens.Length; index++)
                        {
                            if(tokens[index] == "\r")
                            {
                                break;
                            }
                        }
                    }

                }
            }
        }
        

        public void Parse()
        {
            //frm
            //frx
            FormString = File.ReadAllText(Path.Combine(Directory, Name + ".frm"));
            var frxData = File.ReadAllBytes(Path.Combine(Directory, Name + ".frx"));


            var reader = new TokenReader(FormString);

            var builderPartial = new StringBuilder();
            var builderDesigner = new StringBuilder();

            if (reader.StartWhen("Begin"))
            {
                var tokens = reader.TokensBetween("End", "Begin"); // starts at level 1.
                var index = 0;
                if(tokens[index] == "VB.Form")
                {
                    index++;
                    //NET_CustData
                    var DataName = tokens[index];

                    index++;

                    builderPartial.AppendLine($"public partial class {Name} : Form");
                    builderPartial.AppendLine("{");
                    builderPartial.AppendLine($"    public {Name}()");
                    builderPartial.AppendLine(" {");
                    builderPartial.AppendLine("     InitializeComponent();");
                    builderPartial.AppendLine(" }");
                    builderPartial.AppendLine("}");


                    builderDesigner.AppendLine($"partial class {Name}");
                    builderDesigner.AppendLine("{");

                    builderDesigner.AppendLine(@"\t/// <summary>
\t/// Required designer variable.
\t/// </summary>
\tprivate System.ComponentModel.IContainer components = null;

\t/// <summary>
\t/// Clean up any resources being used.
\t/// </summary>
\t/// <param name=""disposing"">true if managed resources should be disposed; otherwise, false.</param>
\tprotected override void Dispose(bool disposing)
\t{
\t\tif (disposing && (components != null))
\t\t{
\t\t\tcomponents.Dispose();
\t\t}
\t\tbase.Dispose(disposing);
\t}".Replace(@"\t", "\t"));

                    builderDesigner.AppendLine();

                    builderDesigner.AppendLine(@"\t#region Windows Form Designer generated code

\t/// <summary>
\t/// Required method for Designer support - do not modify
\t/// the contents of this method with the code editor.
\t/// </summary>
\tprivate void InitializeComponent()
\t{".Replace(@"\t", "\t"));

                    index++;

                    WritePropertyFields(builderDesigner, "this", ref index, ref tokens ,"\t\t");

                    builderDesigner.AppendLine("\t}");

                    builderDesigner.AppendLine();


                    foreach (var item in NameTypePairs)
                    {
                        if(NameCount.ContainsKey(item.Key))
                        {
                            builderDesigner.AppendLine($"\tpublic {item.Value}[] {item.Key} = new {item.Value}[{NameCount[item.Key]}];");
                        }
                        else
                        {
                            builderDesigner.AppendLine($"\tpublic {item.Value} {item.Key};");
                        }
                        
                    }

                    builderDesigner.AppendLine();

                    builderDesigner.AppendLine("\t#endregion");
                    builderDesigner.AppendLine();

                    builderDesigner.AppendLine("}");

                    var x = builderDesigner.ToString();

                }
            }
        }



    }
}
