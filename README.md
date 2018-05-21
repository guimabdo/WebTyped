[![Build status](https://ci.appveyor.com/api/projects/status/github/guimabdo/webtyped?svg=true)](https://ci.appveyor.com/project/guimabdo/webtyped) [![Latest version](https://img.shields.io/npm/v/@guimabdo/webtyped-common.svg)](https://www.npmjs.com/search?q=@guimabdo/webtyped)
# WebTyped

 WebTyped is a tool for generating strongly typed TypeScript code from your http://ASP.NET or http://ASP.NET/core Web Apis.

## Quick Start

```
npm install @guimabdo/webtyped-generator
npm install @guimabdo/webtyped-[fetch|jquery|angular]

```

webpack.config.js:

```javascript
const WebTypedPlugin = require('@guimabdo/webtyped-generator').WebTypedPlugin;
module.exports = {
   plugins: [
		  new WebTypedPlugin({
			  sourceFiles: [
				   "./Controllers/Api/**/*.cs",
				   "./Models/**/*.cs"],
			  serviceMode: "fetch", //or "jquery", or "angular"
			  outDir: "./src/webtyped/",
			  clear: true
		})
	  ]
}
```

Run webpack and use generated services wherever you want:

```typescript
import { MyService } from './webtyped/<services-folder>';
let myService = new MyService(); //Generated from MyController.cs
myService.get().then(result => console.log(result));
```

### Angular? (4.3.0+) Import the generated module and inject services when needed:

app.module.ts

```typescript
import { WebTypedGeneratedModule } from './webtyped';
@NgModule({
	imports: [WebTypedGeneratedModule.forRoot()]
})
export class AppModule {}
```

some.component.ts (for example)
```typescript
import { MyService } from './webtyped/<services-folder>';
@Component({...})
export class SomeComponent {
	constructor(myService: MyService){}
}
```
## WebTyped.Annotations
 [![Latest version](https://img.shields.io/nuget/v/WebTyped.Annotations.svg)](https://www.nuget.org/packages/WebTyped.Annotations/)

Attributes for changing services/models generator behaviour.

### ClientTypeAttribute

Use this attribute for mapping a Server Model to an existing Client Type so it won't be transpiled by the generator. 
- typeName: correspondent client type name, or empty if it has the same name as the server type.
- module: type module, leave it empty if the type is globally visible.

Generated API services will know how to resolve the type.

example:
```C#
[WebTyped.Annotations.ClientType(module: "primeng/components/common/selectitem")]
public class SelectItem { 
    public string Label { get; set; }
    public long Value { get; set; }
}
```


## Requirements

netcore 2.0 on dev machine
