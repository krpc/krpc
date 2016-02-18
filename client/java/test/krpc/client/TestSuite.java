package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

@RunWith(Suite.class)
@Suite.SuiteClasses({ ConnectionTest.class, EncoderTest.class, PerformanceTest.class, RemoteObjectTest.class, StreamTest.class })
public class TestSuite {
    public static void main(String[] args) {
        org.junit.runner.JUnitCore.main("krpc.client.TestSuite");
    }
}
