# CSharpJSON
JSON Data Management

### Basic usage
```c#
import GI;
...

class Test {
  void Main() {
    JSON data = new JSON(JSONType.Object);
    data["string"] = new JSON("Bar");
    data["number"] = new JSON(100);
    data["bool"] = new JSON(true);
    data["null"] = new JSON();
    data.ObjAdd("time", new JSON(DateTime.Now));
    
    MessageBox.Show(data.ToString());
  }
}
```
