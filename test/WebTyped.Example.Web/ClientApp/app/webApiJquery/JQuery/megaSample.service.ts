import { WebApiClient } from '@guimabdo/webtyped-jquery';
export class MegaSampleService extends WebApiClient {
	constructor(baseUrl: string = WebApiClient.baseUrl) {
		super(baseUrl, "api/MegaSample");
	}
	GetThisStringFromQuery = (str: string) : JQuery.jqXHR<string> => {
		return this.invokeGet<string>({
				func: this.GetThisStringFromQuery,
				parameters: { str }
			},
			``,
			{ str }
		);
	};
	GetThisStringFromQueryExplicit = (str: string) : JQuery.jqXHR<string> => {
		return this.invokeGet<string>({
				func: this.GetThisStringFromQueryExplicit,
				parameters: { str }
			},
			`explicit`,
			{ str }
		);
	};
	GetThisStringFromRoute = (str: string) : JQuery.jqXHR<string> => {
		return this.invokeGet<string>({
				func: this.GetThisStringFromRoute,
				parameters: { str }
			},
			`route/${str}`,
			undefined
		);
	};
	GetTheseStrings = (str: string, str2: string) : JQuery.jqXHR<Array<string>> => {
		return this.invokeGet<Array<string>>({
				func: this.GetTheseStrings,
				parameters: { str, str2 }
			},
			`these/${str}`,
			{ str2 }
		);
	};
	PostAndReturnThisStringFromQuery = (str: string) : JQuery.jqXHR<string> => {
		return this.invokePost<string>({
				func: this.PostAndReturnThisStringFromQuery,
				parameters: { str }
			},
			``,
			null,
			{ str }
		);
	};
	PostAndReturnThisStringFromQueryExplicit = (str: string) : JQuery.jqXHR<string> => {
		return this.invokePost<string>({
				func: this.PostAndReturnThisStringFromQueryExplicit,
				parameters: { str }
			},
			`explicit`,
			null,
			{ str }
		);
	};
	PostAndReturnThisStringFromRoute = (str: string) : JQuery.jqXHR<string> => {
		return this.invokePost<string>({
				func: this.PostAndReturnThisStringFromRoute,
				parameters: { str }
			},
			`${str}`,
			null,
			undefined
		);
	};
	PostAndReturnTheseStrings = (str: string, str2: string, str3: string) : JQuery.jqXHR<Array<string>> => {
		return this.invokePost<Array<string>>({
				func: this.PostAndReturnTheseStrings,
				parameters: { str, str2, str3 }
			},
			`these/${str}`,
			str3,
			{ str2 }
		);
	};
	PostAndReturnModel = (model: JQuery.MegaSampleService.Model) : JQuery.jqXHR<JQuery.MegaSampleService.Model> => {
		return this.invokePost<JQuery.MegaSampleService.Model>({
				func: this.PostAndReturnModel,
				parameters: { model }
			},
			`model`,
			model,
			undefined
		);
	};
	PostAndReturnModelSameName = (model: JQuery.ModelA) : JQuery.jqXHR<JQuery.ModelA> => {
		return this.invokePost<JQuery.ModelA>({
				func: this.PostAndReturnModelSameName,
				parameters: { model }
			},
			`modelA1`,
			model,
			undefined
		);
	};
	PostAndReturnModelSameName2 = (model: JQuery.OtherModels.ModelA) : JQuery.jqXHR<JQuery.OtherModels.ModelA> => {
		return this.invokePost<JQuery.OtherModels.ModelA>({
				func: this.PostAndReturnModelSameName2,
				parameters: { model }
			},
			`modelA2`,
			model,
			undefined
		);
	};
	PostAndReturnTuple_NotWorkingYet = (tuple: {str: string, number: number}) : JQuery.jqXHR<{str: string, number: number}> => {
		return this.invokePost<{str: string, number: number}>({
				func: this.PostAndReturnTuple_NotWorkingYet,
				parameters: { tuple }
			},
			`tuple`,
			tuple,
			undefined
		);
	};
}