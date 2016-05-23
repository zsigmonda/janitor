using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Janitor.ProofOfConcept
{
  class Program
  {
    static void Main(string[] args)
    {
      //Analysis semanticAnalysis = new Analysis(System.IO.Path.Combine(Environment.CurrentDirectory, "Input", "Input1.cs"));
      //semanticAnalysis.DoAnalysis();

      Transformation syntaxTransformation = new Transformation(System.IO.Path.Combine(Environment.CurrentDirectory, "Input", "Input1.cs"));
      syntaxTransformation.DoTransformation();
    }
  }
}
