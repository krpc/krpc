package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** Test suite for the kRPC Java client. */
@RunWith(Suite.class)
@Suite.SuiteClasses({ConnectionTest.class, EncoderTest.class, EncoderTestSuite.class,
                     EventTest.class, PerformanceTest.class, RemoteObjectTest.class,
                     StreamTest.class })
public class TestSuite {
  /** Entry point for running the test suite directly. */
  public static void main(String[] args) {
    org.junit.runner.JUnitCore.main("krpc.client.TestSuite");
  }
}
