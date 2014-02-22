namespace KRPC.Server
{
    interface IClientEventArgs
    {
        IClient Client { get; }
    }

    interface IClientEventArgs<TIn,TOut>
    {
        IClient<TIn,TOut> Client { get; }
    }
}

