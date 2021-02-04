.assembly OrphanMethod {}
.assembly extern mscorlib {}

.method static public int32 Add(int32 x, int32 y) cil managed 
{
    .maxstack 2

    ldarg.0
    ldarg.1
    add
    ret
}

.method static public int32 'An awesome method to add 𝓧➕𝓨'(int32 '𝓧', int32 '𝓨') cil managed 
{
    .maxstack 2

    ldarg.0
    ldarg.1
    add
    ret
}

.namespace CleanNamespaceName 
{  
    .namespace SecondLevel
    {
        .class public '𝐌𝐲 𝗮𝘄𝗲𝘀𝗼𝗺𝗲 𝘤𝘭𝘢𝘴𝘴 𝘢𝘯 𝒊𝒏𝒂𝒄𝒄𝒆𝒔𝒔𝒊𝒃𝒍𝒆 𝙣𝙖𝙢𝙚 😭' 
        {
            .method static public int32 '𝑓'(int32 '𝓧', int32 '𝓨') cil managed 
            {
                .maxstack 2

                ldarg.0
                ldarg.1
                add
                ret
            }
            
            .method static public int32 Sum(int32 '𝓧', int32 '𝓨') cil managed 
            {
                .maxstack 2

                ldarg.0
                ldarg.1
                add
                ret
            }
            
            .method static public int32 'Method with . in it'(int32 '𝓧', int32 '𝓨') cil managed 
            {
                .maxstack 2

                ldarg.0
                ldarg.1
                add
                ret
            }
        }

        .namespace 'Deeply wrong .namespace name' 
        {
            .class public 'My generic class'<'Generic Type'> 
            {
                .field private !0 'Generic Item'

                .method public instance void .ctor() 
                {
                    .maxstack 1
                    ldarg.0
                    call instance void System.Object::.ctor()

                    ret
                }
            }

            .class public 'NamespaceA.NamespaceB.ClassName`1+InnerClass`2'
            {
                .method public instance int32 'MethodName`1(System.Int32,System.Int32,System.Int32)'()
                {
                    .maxstack 1

                    ldc.i4.s 0
                    ret
                }
                
                .method public instance int32 'MethodName`2(System.Int32,System.Int32,System.Int32)'(int32 '1st parameter', int32 '2nd parameter')
                {
                    .maxstack 1

                    ldc.i4.s 0
                    ret
                }

                .method public instance int32 'MethodName`3'(int32 '1st parameter', int32 '2nd parameter')
                {
                    .maxstack 1

                    ldc.i4.s 1
                    ret
                }

                .class nested public 'Inner Class'
                {
                    .method public instance int32 'MethodName`3'(int32 '1st parameter', int32 '2nd parameter')
                    {
                        .maxstack 1

                        ldc.i4.s 1
                        ret
                    }
                }
            }
        }
    }
}

.class public 'My generic class'<'Generic Type'> 
{
    .field private !0 'Generic Item'

    .method public instance void .ctor() 
    {
        .maxstack 1
        ldarg.0
        call instance void System.Object::.ctor()

        ret
    }
}

.class public 'NamespaceA.NamespaceB.ClassName`1+InnerClass`2'
{
    .method public instance int32 'MethodName`2(System.Int32,System.Int32,System.Int32)'()
    {
        .maxstack 1

        ldc.i4.s 0
        ret
    }

    .method public instance int32 'MethodName`3'(int32 '1st parameter', int32 '2nd parameter')
    {
        .maxstack 1

        ldc.i4.s 1
        ret
    }
}