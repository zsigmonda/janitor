using System;

namespace Test
{
  public class SimpleWorker
  {
    public void A()
    {
      try
      {
        int x = 5;
      }
      catch (Exception ex)
      {
        return;
      }
      finally
      {
        return;
      }
    }
  }

  /*
  public class Worker
  {
    Worker internalWorker;
    // Create a delegate.
    delegate void Del(int x);

    public void A()
    {
      B();
      C();
      C();
      C();
      D();
      Worker w = new Worker();
      w.C();
      w.internalWorker.D();
    }

    public void B()
    {
      (x) => { return x + 5; };
      (y) =>
      {
        int t = 1;
        try
        {
          t = 1 / y;
        }
        catch
        {
          return 0;
        }
        return t;
      };
    }

    public void C()
    {
      Del d = delegate (int k) { Console.WriteLine(k - 1); };
    }

    public void D()
    {
      Del d = delegate (int k) { Console.WriteLine(k + 1); };
    }

    public void E()
    {

    }

  }

  public class Kacsa
  {
    public System.IO.StreamWriter SW { get; private set; }

    public Kacsa()
    {
      SW = new System.IO.StreamWriter("");
    }
  }
  */
}
