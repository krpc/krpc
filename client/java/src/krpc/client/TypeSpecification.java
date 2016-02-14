package krpc.client;

public class TypeSpecification {
    private Class<?> type;
    private TypeSpecification[] genericTypes;

    public TypeSpecification(Class<?> type) {
        this.type = type;
    }

    public TypeSpecification(Class<?> type, TypeSpecification... genericTypes) {
        this.type = type;
        this.genericTypes = genericTypes;
    }

    public Class<?> getType() {
        return type;
    }

    public TypeSpecification[] getGenericTypes() {
        return genericTypes;
    }
}
