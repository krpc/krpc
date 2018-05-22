file(READ ${input} contents)
string(REPLACE "#include \"krpc.pb.h\"" "#include \"krpc/krpc.pb.hpp\"" contents "${contents}")
file(WRITE ${output} "${contents}")
