package krpc.client;

/** Abstract base class for services. */
public abstract class RemoteService {
  protected final Connection connection;

  /** Creates a new RemoteService for the given connection. */
  protected RemoteService(Connection connection) {
    this.connection = connection;
  }
}
