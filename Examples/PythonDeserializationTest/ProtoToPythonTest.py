import google.protobuf as pb
# run protoc on console in order to generate the python proxy:
# protoc -I="<path to source proto file dir>" --python_out="<dest dir>" "<source proto file>.proto"
import TestStructSmall1_pb2

s1 = TestStructSmall1_pb2.ProtobufColectionWrapper_TestStructSmall1()
f = open("TestStructSmall1_proto.dat", "rb")
s1.ParseFromString(f.read())
f.close()
print (s1)



