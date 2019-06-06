import { Injectable } from '@angular/core';
import { HttpClient, HttpRequest, HttpErrorResponse, HttpEvent, HttpEventType } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class RequestService {

  private nameserver = 'http://188.130.155.59:33033/';

  constructor(private http: HttpClient) { }

  getDirectories(path, user: any): Observable<any> {
    const formData: FormData = new FormData();
    formData.append('path', '/');
    return this.http.get(this.nameserver + `list?path=${path}&user=${user}`);
  }

  createDirectory(path, user: any): Observable<any> {
    return this.http.get(this.nameserver + `createDir?path=${path}&user=${user}`).pipe(
      catchError(this.handleError)
    );
  }

  deleteDirectory(path, user: any): Observable<any> {
    return this.http.get(this.nameserver + `delDir?path=${path}&user=${user}`);
  }
  deleteFile(path, user: any): Observable<any> {
    return this.http.get(this.nameserver + `delFile?path=${path}&user=${user}`);
  }

  uploadFileNameServer(size: any, user: any): Observable<any> {
    return this.http.get(this.nameserver + `uploadFile?size=${size}&user=${user}`);
  }

  authUser(user: any): Observable<any> {
    return this.http.get(this.nameserver + `regUser?user=${user}`);
  }

  uploadFileStorage(apiUrl: any, fileToUpload: File, path: any, name: any, user: any) {
    const endpoint = 'http://' + apiUrl + `/api/client/upload?path=${path}&name=${name}&user=${user}`;
    // const endpoint = `http://188.130.155.58:9000/api/client/upload?path=${path}&name=${name}&user=${user}`;
    const formData: FormData = new FormData();
    formData.append("file", fileToUpload);
    const req = new HttpRequest('POST', endpoint, formData, {
      reportProgress: true
    });
    // return this.http.post(endpoint,formData).pipe(
    //   catchError(this.handleError)
    // );
    return this.http.request(req).pipe(
      catchError(this.handleError)
    );
  }

  // rewriteFileStorage(apiUrl:any,fileToUpload: File,path: any,name: any): Observable<any> {
  //   const endpoint =  'http://' + apiUrl+`/api/client/rewrite?path=${path}&name=${name}`;
  //   const formData: FormData = new FormData();
  //   formData.append("file", fileToUpload);
  //   // return this.http.post(endpoint,formData).pipe(
  //   //   catchError(this.handleError)
  //   // );
  //   const req = new HttpRequest('POST', endpoint, formData, {
  //     reportProgress: true
  //   });
  //   return this.http.request(req).pipe(
  //     catchError(this.handleError)
  //   );
  // }

  downloadFile(apiUrl: any, apiUrl2: any, path: any, user: any): Observable<any> {

    return this.http.get('http://' + apiUrl + '/api/client/read?path=/' + path + `&user=${user}`, { responseType: 'blob' as 'json' }).pipe(
      catchError(err => {
        return this.http.get('http://' + apiUrl2 + '/api/client/read?path=/' + path + `&user=${user}`, { responseType: 'blob' as 'json' });
      }))
  }

  downloadLogs(user: any): Observable<any> {
    return this.http.get(this.nameserver + `/getLog?user=${user}`);
  }

  private handleError(error: HttpErrorResponse) {
    if (error.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', error.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong,
      console.error(
        `Backend returned code ${error.status}, ` +
        `body was: ${error.error}`);
    }
    // return an observable with a user-facing error message
    return throwError('Something bad happened; please try again later.');
  };

}
