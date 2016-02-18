package krpc.client;

import java.io.IOException;

public class Stream<T> {
    private StreamManager manager;
    private int id;

    Stream(StreamManager manager, int id) {
        this.manager = manager;
        this.id = id;
    }

    @SuppressWarnings({ "unchecked" })
    public T get() throws StreamException, RPCException, IOException {
        return (T) manager.get(id);
    }

    public void remove() throws RPCException, IOException {
        manager.remove(id);
    }
}
