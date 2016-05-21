using System;

/*
// Ha ezt itt engedélyezem, nem találja meg a normál system névteret.
namespace Test.System
{
  public interface IDisposable
  {
    public void Dispose();
  }
}
*/

namespace Test
{
  public class NoDisposeNeeded
  {
    public NoDisposeNeeded()
    {
      Console.WriteLine("Nincs dispose");
    }

    public void Foo()
    {
      Console.WriteLine("foo");
    }
  }

  /*
  public class FakeDisposeNeeded : Test.System.IDisposable
  {
    public FakeDisposeNeeded()
    {
      Console.WriteLine("Kamu");
    }

    public void Foo()
    {
      Console.WriteLine("foo");
    }

    public void Dispose()
    {
      Console.WriteLine("Kamu dispose");
    }
  }
  */

  public class DisposeNeeded : IDisposable
  {
    public DisposeNeeded()
    {
      Console.WriteLine("Van dispose");
    }

    public void Foo()
    {
      Console.WriteLine("foo");
    }

    public void Dispose()
    {
      Console.WriteLine("Ez a dispose");
    }
  }

  public class Worker
  {
    DisposeNeeded field1;
    DisposeNeeded field2 = new DisposeNeeded();
    System.IO.StreamReader field3;
    //FakeDisposeNeeded field3;
    DisposeNeeded field5 = new DisposeNeeded();

    public void DoWork()
    {
      int x = 5;
      x += 6;
      NoDisposeNeeded object1 = new NoDisposeNeeded();
      DisposeNeeded object2 = new DisposeNeeded();
      System.IO.StreamReader object3 = new System.IO.StreamReader("");
      Object object4 = new Object();
      field1.Foo();
      field2.Foo();
      NoDisposeNeeded object5 = CreateNonDisposable();
      DisposeNeeded object6 = CreateDisposable();
      object3.Dispose();
      object6.Dispose();
      field2.Dispose();
      return;
      field5.Dispose();
      object4 = object3;
      object3 = null;

      using (System.IO.StreamReader sr = new System.IO.StreamReader(""))
      {
        sr.ReadToEnd();
      }

      System.IO.StreamReader sr2 = new System.IO.StreamReader("");
      using (sr2 = sr)
      {
        sr2.ReadToEnd();
      }

      System.IO.StreamReader sr3;
      using (sr3 = new System.IO.StreamReader(""))
      {
        sr3.ReadToEnd();
      }

      using (sr3)
      {
        sr2.ReadToEnd();
      }
    }

    public DisposeNeeded CreateDisposable()
    {
      return new DisposeNeeded();
    }

    public NoDisposeNeeded CreateNonDisposable()
    {
      return new NoDisposeNeeded();
    }
  }
}
