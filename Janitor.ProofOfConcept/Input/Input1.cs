using System;

namespace Test
{
  public class Worker
  {
    public void A()
    {
      try
      {
        B();
        C();
        D();
      }
      catch (Exception ex)
      {
        ;
      }
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

    }

    public void D()
    {

    }

    public void E()
    {

    }

  }
}
