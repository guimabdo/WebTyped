﻿import { WebApiCallInfo } from '@guimabdo/webtyped-common';
import * as $ from 'jquery';
class FakeXhr<T> extends Promise<T> {
    state: () => "pending" | "resolved" | "rejected" = () => "pending";
    statusCode = () => 0;
    always = () => this;
    fail = () => this;
    done = () => this;
    progress = () => this;
    promise = () => this;
    constructor() {
        super((res, rej) => res(null));
    }
}
export class WebApiClient {
    //Global setting
    public static baseUrl: string = null;
    public static api: string = null;
    constructor(private baseUrl: string = WebApiClient.baseUrl,
        private api: string = WebApiClient.api) {
        this.baseUrl = this.baseUrl || "/";
        this.api = this.api || "";
    }
    invokeGet<T>(info: WebApiCallInfo, action: string, search?: any): JQuery.jqXHR<T> {
        return this.invoke(info, action, 'get', null, search);
    }
    invokePatch<T>(info: WebApiCallInfo, action: string, body?: any, search?: any): JQuery.jqXHR<T> {
        return this.invoke(info, action, 'patch', body, search);
    }
    invokePost<T>(info: WebApiCallInfo, action: string, body?: any, search?: any): JQuery.jqXHR<T> {
        return this.invoke(info, action, 'post', body, search);
    }
    invokePut<T>(info: WebApiCallInfo, action: string, body?: any, search?: any): JQuery.jqXHR<T> {
        return this.invoke(info, action, 'put', body, search);
    }
    invokeDelete<T>(info: WebApiCallInfo, action: string, search?: any): JQuery.jqXHR<T> {
        return this.invoke(info, action, 'delete', null, search);
    }
    private invoke<T>(info: WebApiCallInfo, action: string,
        httpMethod: string, body?: any, search?: any): JQuery.jqXHR<T> {
        if (typeof ($.ajax) === 'undefined') {
            var anyFake: any = new FakeXhr<T>();
            return <JQuery.jqXHR<T>>anyFake;
        };
        var baseUrl = this.baseUrl;
        if (baseUrl.endsWith('/')) { baseUrl = baseUrl.substr(0, baseUrl.length - 1); }
        var url = `${baseUrl}/${this.api}/${action}`;
        if (search) {
            if (url.indexOf('?') < 0) {
                url += '?';
            } else {
                url += '&';
            }
            url += $.param(search);
        }
        return $.ajax({
            url: url,
            dataType: 'json',
            contentType: 'application/json',
            data: body ? JSON.stringify(body) : undefined,
            method: httpMethod,
        });
    }
}