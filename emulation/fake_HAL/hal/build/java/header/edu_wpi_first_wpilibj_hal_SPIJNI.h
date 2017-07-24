/* DO NOT EDIT THIS FILE - it is machine generated */
#include <jni.h>
/* Header for class edu_wpi_first_wpilibj_hal_SPIJNI */

#ifndef _Included_edu_wpi_first_wpilibj_hal_SPIJNI
#define _Included_edu_wpi_first_wpilibj_hal_SPIJNI
#ifdef __cplusplus
extern "C" {
#endif
/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiInitialize
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiInitialize
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiTransaction
 * Signature: (ILjava/nio/ByteBuffer;Ljava/nio/ByteBuffer;B)I
 */
JNIEXPORT jint JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiTransaction
  (JNIEnv *, jclass, jint, jobject, jobject, jbyte);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiWrite
 * Signature: (ILjava/nio/ByteBuffer;B)I
 */
JNIEXPORT jint JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiWrite
  (JNIEnv *, jclass, jint, jobject, jbyte);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiRead
 * Signature: (ILjava/nio/ByteBuffer;B)I
 */
JNIEXPORT jint JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiRead
  (JNIEnv *, jclass, jint, jobject, jbyte);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiClose
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiClose
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetSpeed
 * Signature: (II)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetSpeed
  (JNIEnv *, jclass, jint, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetOpts
 * Signature: (IIII)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetOpts
  (JNIEnv *, jclass, jint, jint, jint, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetChipSelectActiveHigh
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetChipSelectActiveHigh
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetChipSelectActiveLow
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetChipSelectActiveLow
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiInitAccumulator
 * Signature: (IIIBIIBBZZ)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiInitAccumulator
  (JNIEnv *, jclass, jint, jint, jint, jbyte, jint, jint, jbyte, jbyte, jboolean, jboolean);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiFreeAccumulator
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiFreeAccumulator
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiResetAccumulator
 * Signature: (I)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiResetAccumulator
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetAccumulatorCenter
 * Signature: (II)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetAccumulatorCenter
  (JNIEnv *, jclass, jint, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiSetAccumulatorDeadband
 * Signature: (II)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiSetAccumulatorDeadband
  (JNIEnv *, jclass, jint, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiGetAccumulatorLastValue
 * Signature: (I)I
 */
JNIEXPORT jint JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiGetAccumulatorLastValue
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiGetAccumulatorValue
 * Signature: (I)J
 */
JNIEXPORT jlong JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiGetAccumulatorValue
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiGetAccumulatorCount
 * Signature: (I)I
 */
JNIEXPORT jint JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiGetAccumulatorCount
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiGetAccumulatorAverage
 * Signature: (I)D
 */
JNIEXPORT jdouble JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiGetAccumulatorAverage
  (JNIEnv *, jclass, jint);

/*
 * Class:     edu_wpi_first_wpilibj_hal_SPIJNI
 * Method:    spiGetAccumulatorOutput
 * Signature: (ILjava/nio/LongBuffer;Ljava/nio/LongBuffer;)V
 */
JNIEXPORT void JNICALL Java_edu_wpi_first_wpilibj_hal_SPIJNI_spiGetAccumulatorOutput
  (JNIEnv *, jclass, jint, jobject, jobject);

#ifdef __cplusplus
}
#endif
#endif
