package krpc.client;

/**
 * An error caused when a kRPC stream fails.
 */
public class StreamException extends Exception {
    private static final long serialVersionUID = 54119455204332164L;

    public StreamException(String message) {
        super(message);
    }
}
