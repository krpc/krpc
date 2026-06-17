package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** Test suite for the Encoder test cases. */
@RunWith(Suite.class)
@Suite.SuiteClasses({EncoderSint32ValueTest.class, EncoderSint64ValueTest.class,
                     EncoderUint32ValueTest.class, EncoderUint64ValueTest.class,
                     EncoderSingleValueTest.class, EncoderDoubleValueTest.class,
                     EncoderBooleanValueTest.class, EncoderStringValueTest.class,
                     EncoderBytesValueTest.class, EncoderListCollectionTest.class,
                     EncoderDictionaryCollectionTest.class, EncoderSetCollectionTest.class})
public class EncoderTestSuite {
}
