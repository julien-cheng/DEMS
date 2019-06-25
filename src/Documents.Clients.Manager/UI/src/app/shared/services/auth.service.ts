import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import {IBatchResponse} from '../index';

export interface IUser {
    id: number;
    firstName: string;
    lastName: string;
    userName: string;
}

@Injectable()
export class AuthService {
    public currentUser: IUser;
    public readOnly: boolean;
    
    // private headers = new HttpHeaders({ 'Content-Type': 'application/json' }).set('Authorization', 'my-auth-token');
    // {headers: new HttpHeaders().set('Authorization', 'my-auth-token')} /// TBD
    private defaultLoginUrl = '/manager/';
    private postLoginUrl: string = this.defaultLoginUrl;
    constructor(private http: HttpClient) {
    }

    // Description: check to see if user has auth to see this page and check read/write rights
    loggedIn() {
        // console.log('check if user is logged in');
        //  ***** Working - TBD
        // Return read-only flag - get rights from server call
        this.readOnly = false;
        return true;
    }
    getPostLoginUrl(): string {
        return this.postLoginUrl;
    }

    getDefaultLoginUrl(): string {
        return this.defaultLoginUrl;
    }

    setPostLoginUrl(url: string): void {
        this.postLoginUrl = url;
    }

    // Description: basic log in method
    loginUser(userName: string, password: string): Observable<IUser> {
        const loginInfo = { username: userName, password }; // body
        let options = { headers: new HttpHeaders().set('Authorization', 'my-auth-token') }
        return this.http.post<IUser>('api/login', loginInfo, options).do((response) => {
            if (response) {
                this.currentUser = <IUser>response;
            }
        });
    }

    // Description: Authenticate recipients of ediscovery materials
    ediscoveryAuthenticateUser(formValues: {email: string, password: string, token: string}): Observable<IBatchResponse> {
        const loginInfo = { 
            email: formValues.email, 
            password: formValues.password, 
            token: encodeURIComponent(formValues.token), 
            type: 'AuthenticateUserRequest' };
        let options = { headers: new HttpHeaders().set('Authorization', 'my-auth-token') }
        return this.http.post<IBatchResponse>('api/ediscovery/authenticateuser', loginInfo, options).do((result:IBatchResponse) => {
            if (result) {
                return result.response;
            }
        });
    }

    // Description: Authenticate recipients of leo section
    leoAuthenticateUser(formValues: {email: string, password: string, token: string}): Observable<IBatchResponse> {
        const loginInfo = { 
            email: formValues.email, 
            password: formValues.password, 
            token: encodeURIComponent(formValues.token), 
            type: 'AuthenticateUserRequest' };
        let options = { headers: new HttpHeaders().set('Authorization', 'my-auth-token') }
        return this.http.post<IBatchResponse>('api/leoupload/authenticateuser', loginInfo, options).do((result:IBatchResponse) => {
            if (result) {
                return result.response;
            }
        });
    }


    // checkAutheticationStatus() {
    //     console.log('checkAutheticationStatus ');
    //     return this.http.get('/api/Folder/?folderKey=Defendant:14015529').map((response: any) => {
    //         if (response._body) {
    //             return response.json();
    //         } else {
    //             return {};
    //         }
    //     }).do((currentUser) => {
    //         if (!!currentUser.userName) {
    //             this.currentUser = currentUser;
    //         }
    //     }).subscribe();
    // }

    // isAuthenticated() {
    //     if (this.currentUser !== undefined) {
    //         console.log(this.currentUser);
    //     }

    //     return !!this.currentUser;
    // }

    // updateCurrentUser(firstName: string, lastName: string) {
    //     this.currentUser.firstName = firstName;
    //     this.currentUser.lastName = lastName;

    //     const headers = new Headers({ 'Content-type': 'application/json' });
    //     const options = new RequestOptions({ headers });

    //     return this.http.put(`/api/users/${this.currentUser.id}`, JSON.stringify(this.currentUser), options);
    // }




    // logout() {
    //     // Logout the user on the client side
    //     this.currentUser = undefined;

    //     const headers = new Headers({ 'Content-type': 'application/json' });
    //     const options = new RequestOptions({ headers });

    //     return this.http.post('/api/logout', JSON.stringify({}), options);
    // }

    // handleError(error: Response) {
    //     return Observable.throw(error.statusText);
    // }
}
