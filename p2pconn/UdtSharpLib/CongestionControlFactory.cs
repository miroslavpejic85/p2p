namespace UdtSharp
{

    public abstract class CCVirtualFactory
    {
        public abstract CC create();
        public abstract CCVirtualFactory clone();
    }

    public class CCFactory<T> : CCVirtualFactory where T : new()
    {
        public override CC create()
        {
            return new T() as CC;
        }

        public override CCVirtualFactory clone()
        {
            return new CCFactory<T>();
        }
    }
}