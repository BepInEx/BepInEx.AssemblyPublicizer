using TestLibrary;

var internalClass = new InternalClass();
internalClass.PrivateMethod();
Console.WriteLine(internalClass.PrivateProperty);
internalClass.PrivateAutoProperty = 123;
Console.WriteLine(internalClass.PrivateAutoProperty);
