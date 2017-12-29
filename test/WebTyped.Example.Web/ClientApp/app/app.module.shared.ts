import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './components/app/app.component';
import { NavMenuComponent } from './components/navmenu/navmenu.component';
import { HomeComponent } from './components/home/home.component';
import { FetchDataComponent } from './components/fetchdata/fetchdata.component';
import { CounterComponent } from './components/counter/counter.component';
import { MegaSampleComponent } from './components/megaSample/megaSample.component';
import * as webApi from './webApi/';
//import { WebApiInterceptor } from '@guimabdo/webtyped-angular';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    declarations: [
        AppComponent,
        NavMenuComponent,
        CounterComponent,
        FetchDataComponent,
        HomeComponent,
        MegaSampleComponent
    ],
    imports: [
        CommonModule,
        HttpModule,
        HttpClientModule,
        FormsModule,
        RouterModule.forRoot([
            { path: '', redirectTo: 'home', pathMatch: 'full' },
            { path: 'home', component: HomeComponent },
            { path: 'counter', component: CounterComponent },
            { path: 'fetch-data', component: FetchDataComponent },
            { path: 'megaSample', component: MegaSampleComponent },
            { path: '**', redirectTo: 'home' }
        ]
        ),
        webApi.WebTypedGeneratedModule.forRoot()
    ],
    providers: [
        //{
        //    provide: HTTP_INTERCEPTORS,
        //    useClass: WebApiInterceptor,
        //    multi: true,
        //},
        //...webApi.providers
    ]
})
export class AppModuleShared {
}
