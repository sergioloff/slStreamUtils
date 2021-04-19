# Copyright (c) 2021, Sergio Loff
# All rights reserved.
# This source code is licensed under the BSD-style license found in the
# LICENSE file in the root directory of this source tree.

import google.protobuf as pb

# run protoc on console in order to generate the python proxy:
# protoc -I="<path to source proto file dir>" --python_out="<dest dir>" "<source proto file>.proto"
import TestClass_pb2

s1 = TestClass_pb2.ArrayWrapper_TestClass()
f = open("TestClassCollection_proto.dat", "rb")
s1.ParseFromString(f.read())
f.close()
print (s1)



