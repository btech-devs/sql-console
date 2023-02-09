import { Injectable } from '@angular/core';
import {BaseService} from '../_base/base.service';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Response} from '../_models/responses/base/response';

@Injectable()
export class MetadataService extends BaseService {

  readonly endpoint: string = '/api/metadata';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  getClientId(): Observable<Response<string>> {
    return this.requestGet(`${this.endpoint}/client-id`);
  }

  getJwtPublicKey(): Observable<Response<string>> {
    return this.requestGet(`${this.endpoint}/jwt-public-key`);
  }
}