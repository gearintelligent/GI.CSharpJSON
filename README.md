# CSharpJSON
JSON Data Management

### Basic usage
```c#
import GI;
...

class Test {
  void Main() {
    // Init
    JSON data = new JSON(JSONType.Object);
    
    // Set
    data["string"] = new JSON("Bar");
    data["number"] = new JSON(100);
    data["bool"] = new JSON(true);
    data["null"] = new JSON();
    data.ObjAdd("time", new JSON(DateTime.Now));
    
    // Get
    int num = data["number"].NumberValue;
    string str = data["string"].StringValue;
    bool bol = data["bool"].BoolValue;
    object obj = data["null"].Value;
  }
}
```

### Basic deserialize / parse
```c#
import GI;
...

class Test {
  void Main() {
    string jstr = "{\"foo\": \"bar\", \"num\": 5}";
    JSON data = JSON.Parse(jstr);
  }
}
```

### Basic serialize / encode
```c#
import GI;
...

class Test {
  void Main() {
    JSON data = new JSON("foo");
    MessageBox.Show(data.ToString());
  }
}
```

Contribute are welcome XD
