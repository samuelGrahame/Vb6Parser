using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vb6Parser
{
    public class TokenReader
    {
        public string Data;
        public int Index;
        public TokenReader( string data)
        {
            Data = data;
        }

        public string ReadToken()
        {
            var builder = new StringBuilder();
            int i = Index;
            var inQuote = false;

            for (; i < Data.Length; i++)
            {
                if(inQuote)
                {
                    if(Data[i] == '"')
                    {                        
                        builder.Append(Data[i]);
                        i++;
                        break;
                    }
                    else
                    {
                        builder.Append(Data[i]);
                    }                    
                }
                else
                {                    
                    if(Data[i] == '"')
                    {
                        if(i == Index)
                        {
                            inQuote = true;
                            builder.Append(Data[i]);
                        }
                        else
                        {
                            i--;
                            break;
                        }                        
                    }
                    else
                    {
                        if (char.IsSymbol(Data[i]) || char.IsWhiteSpace(Data[i]))
                        {
                            if(i == Index)
                            {
                                if(Data[i] == '\r' || char.IsSymbol(Data[i]))
                                {
                                    builder.Append(Data[i]);
                                }
                                i++;
                            }
                            else
                            {
                                if (!(Data[i] == '\r' || char.IsSymbol(Data[i])))
                                {
                                    i++;
                                }
                            }
                            
                            break;
                        }
                        builder.Append(Data[i]);
                    }
                    
                }                
            }

            Index = i;

            return builder.ToString();
        }

        public bool StartWhen(string token)
        {
            var x = "";
            while((x = ReadToken()) != token) {
                if (IsEOF())
                    return false;            
            }

            return !IsEOF();
        }

        public string[] TokensBetween(string stopToken, string IncrementLevel, bool includeWhiteSpace = false)
        {
            var list = new List<string>();
            var tokenF = "";
            var level = 1;
            while (true) {
                
                
                if ((tokenF = ReadToken()) == stopToken)
                {
                    level--;
                    if(level == 0)
                    {                        
                        list.Add(tokenF);

                        break;
                    }
                }else if(tokenF == IncrementLevel)
                {
                    level++;
                }

                if(tokenF != "" || includeWhiteSpace)
                {
                    list.Add(tokenF);

                }                
                
            }

            return list.ToArray();
        }

        public bool IsEOF()
        {
            return Index >= Data.Length;
        }
    }
}
