namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class SelfCommand : IResolverCommand {
            public static SelfCommand instance = new SelfCommand();

            public ICommandState CreateState() => new SingleState();

            SelfCommand() { }

            public void Reset(ICommandState state, object from) {
                if (state is SingleState s) s.Reset(from);
            }

            public void Next(ICommandState state) {
                if (state is SingleState s) s.Next();
            }
        }
    }
}