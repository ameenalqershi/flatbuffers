/*
 * Copyright 2026 Ameen Alqershi
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Google.FlatBuffers.Test
{
    [FlatBuffersTestClass]
    public class VerifierNestedBufferOffsetTests
    {
        private static byte[] BuildNestedVectorTable(uint vectorRelativeOffset, short fieldOffset = 4)
        {
            // Root at [0..3], vtable at [4..9], table at [12..19].
            // An optional nested/flexbuffer vector field uses vtable slot 4.
            byte[] buffer = new byte[48];
            BitConverter.GetBytes(12).CopyTo(buffer, 0);
            BitConverter.GetBytes((ushort)6).CopyTo(buffer, 4);
            BitConverter.GetBytes((ushort)8).CopyTo(buffer, 6);
            BitConverter.GetBytes(fieldOffset).CopyTo(buffer, 8);
            BitConverter.GetBytes(8).CopyTo(buffer, 12);
            BitConverter.GetBytes(vectorRelativeOffset).CopyTo(buffer, 16);
            // The vector at [24..27] has zero elements (legal).
            return buffer;
        }

        private static bool VerifyFlexVector(byte[] buffer)
        {
            var verifier = new Verifier(new ByteBuffer(buffer));
            // Matches generated verification for a table with a flexbuffer field.
            return verifier.VerifyBuffer(null, false, (v, table) =>
                v.VerifyTableStart(table) &&
                v.VerifyNestedBuffer(table, 4, null, false) &&
                v.VerifyTableEnd(table));
        }

        [FlatBuffersTestMethod]
        public void ValidNestedVectorOffsetIsAccepted()
        {
            Assert.IsTrue(VerifyFlexVector(BuildNestedVectorTable(8)));
        }

        [FlatBuffersTestMethod]
        public void MissingOptionalNestedVectorIsAccepted()
        {
            Assert.IsTrue(VerifyFlexVector(BuildNestedVectorTable(0, 0)));
        }

        [FlatBuffersTestMethod]
        public void SelfReferentialNestedVectorOffsetIsRejected()
        {
            Assert.IsFalse(VerifyFlexVector(BuildNestedVectorTable(0)));
        }

        [FlatBuffersTestMethod]
        public void WrappedBackwardNestedVectorOffsetIsRejected()
        {
            // Field is at 16; 0xFFFFFFF0 wraps the computed target to 0.
            Assert.IsFalse(VerifyFlexVector(BuildNestedVectorTable(unchecked((uint)-16))));
        }

        [FlatBuffersTestMethod]
        public void NestedVectorFieldOutsideBufferIsRejectedWithoutReading()
        {
            // The vtable field slot references table+40, outside a 48-byte buffer.
            Assert.IsFalse(VerifyFlexVector(BuildNestedVectorTable(0, 40)));
        }

        [FlatBuffersTestMethod]
        public void NestedVectorOffsetOutsideBufferIsRejected()
        {
            Assert.IsFalse(VerifyFlexVector(BuildNestedVectorTable(200)));
        }
    }
}
