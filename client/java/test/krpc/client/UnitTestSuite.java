package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/**
 * What the client does regardless of the transport it talks over, and without a server to
 * talk to: encoding and decoding the values a call carries.
 */
@RunWith(Suite.class)
@Suite.SuiteClasses({EncoderTest.class, EncoderTestSuite.class})
public class UnitTestSuite {
  /** Entry point for running the test suite directly.
   *
   * @param args Command line arguments.
   */
  public static void main(String[] args) {
    org.junit.runner.JUnitCore.main("krpc.client.UnitTestSuite");
  }
}
