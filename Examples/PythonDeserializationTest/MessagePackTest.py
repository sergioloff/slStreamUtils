# Copyright (c) 2021, Sergio Loff
# All rights reserved.
# This source code is licensed under the BSD-style license found in the
# LICENSE file in the root directory of this source tree.
import msgpack

print('sequence of MessagePack objects:')
with open("TestClassCollection_msgPack.dat", "rb") as data_file:
    byte_data = data_file.read()
unpacker = msgpack.Unpacker(use_list=False, raw=False)
unpacker.feed(byte_data)
for unpacked in unpacker:
    print(unpacked)


print('Single "TestClassMPContainer" MessagePack object:')
with open("TestClassContainer_msgPack.dat", "rb") as data_file:
    byte_data = data_file.read()
unpacked = msgpack.unpackb(byte_data)
print(unpacked)
