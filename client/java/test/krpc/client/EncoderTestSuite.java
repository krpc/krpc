package krpc.client;

import org.junit.runner.RunWith;
import org.junit.runners.Suite;

@RunWith(Suite.class)
@Suite.SuiteClasses({EncoderSInt32ValueTest.class, EncoderSInt64ValueTest.class,
                     EncoderUInt32ValueTest.class, EncoderUInt64ValueTest.class,
                     EncoderSingleValueTest.class, EncoderDoubleValueTest.class,
                     EncoderBooleanValueTest.class, EncoderStringValueTest.class,
                     EncoderBytesValueTest.class, EncoderListCollectionTest.class,
                     EncoderDictionaryCollectionTest.class, EncoderSetCollectionTest.class})
public class EncoderTestSuite {
}
