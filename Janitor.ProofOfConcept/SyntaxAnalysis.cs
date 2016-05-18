using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;

namespace Janitor.ProofOfConcept
{
  public class SyntaxAnalysis
  {
    private string m_sourceCode;

    public SyntaxAnalysis(string inputFileName)
    {
      using (StreamReader sr = new StreamReader(inputFileName))
      {
        m_sourceCode = sr.ReadToEnd();
      }
    }

    public void DoAnalysis()
    {
      ;
    }
  }
}
