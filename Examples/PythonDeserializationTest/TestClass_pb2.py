# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: TestClass.proto
"""Generated protocol buffer code."""
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='TestClass.proto',
  package='',
  syntax='proto3',
  serialized_options=None,
  create_key=_descriptor._internal_create_key,
  serialized_pb=b'\n\x0fTestClass.proto\"3\n\x16\x41rrayWrapper_TestClass\x12\x19\n\x05\x41rray\x18\x01 \x03(\x0b\x32\n.TestClass\";\n\tTestClass\x12\n\n\x02i1\x18\x01 \x01(\x05\x12\n\n\x02i2\x18\x02 \x01(\x05\x12\n\n\x02\x62\x31\x18\x03 \x01(\x08\x12\n\n\x02l1\x18\x04 \x01(\x03\x62\x06proto3'
)




_ARRAYWRAPPER_TESTCLASS = _descriptor.Descriptor(
  name='ArrayWrapper_TestClass',
  full_name='ArrayWrapper_TestClass',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  create_key=_descriptor._internal_create_key,
  fields=[
    _descriptor.FieldDescriptor(
      name='Array', full_name='ArrayWrapper_TestClass.Array', index=0,
      number=1, type=11, cpp_type=10, label=3,
      has_default_value=False, default_value=[],
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR,  create_key=_descriptor._internal_create_key),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=19,
  serialized_end=70,
)


_TESTCLASS = _descriptor.Descriptor(
  name='TestClass',
  full_name='TestClass',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  create_key=_descriptor._internal_create_key,
  fields=[
    _descriptor.FieldDescriptor(
      name='i1', full_name='TestClass.i1', index=0,
      number=1, type=5, cpp_type=1, label=1,
      has_default_value=False, default_value=0,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR,  create_key=_descriptor._internal_create_key),
    _descriptor.FieldDescriptor(
      name='i2', full_name='TestClass.i2', index=1,
      number=2, type=5, cpp_type=1, label=1,
      has_default_value=False, default_value=0,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR,  create_key=_descriptor._internal_create_key),
    _descriptor.FieldDescriptor(
      name='b1', full_name='TestClass.b1', index=2,
      number=3, type=8, cpp_type=7, label=1,
      has_default_value=False, default_value=False,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR,  create_key=_descriptor._internal_create_key),
    _descriptor.FieldDescriptor(
      name='l1', full_name='TestClass.l1', index=3,
      number=4, type=3, cpp_type=2, label=1,
      has_default_value=False, default_value=0,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR,  create_key=_descriptor._internal_create_key),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=72,
  serialized_end=131,
)

_ARRAYWRAPPER_TESTCLASS.fields_by_name['Array'].message_type = _TESTCLASS
DESCRIPTOR.message_types_by_name['ArrayWrapper_TestClass'] = _ARRAYWRAPPER_TESTCLASS
DESCRIPTOR.message_types_by_name['TestClass'] = _TESTCLASS
_sym_db.RegisterFileDescriptor(DESCRIPTOR)

ArrayWrapper_TestClass = _reflection.GeneratedProtocolMessageType('ArrayWrapper_TestClass', (_message.Message,), {
  'DESCRIPTOR' : _ARRAYWRAPPER_TESTCLASS,
  '__module__' : 'TestClass_pb2'
  # @@protoc_insertion_point(class_scope:ArrayWrapper_TestClass)
  })
_sym_db.RegisterMessage(ArrayWrapper_TestClass)

TestClass = _reflection.GeneratedProtocolMessageType('TestClass', (_message.Message,), {
  'DESCRIPTOR' : _TESTCLASS,
  '__module__' : 'TestClass_pb2'
  # @@protoc_insertion_point(class_scope:TestClass)
  })
_sym_db.RegisterMessage(TestClass)


# @@protoc_insertion_point(module_scope)