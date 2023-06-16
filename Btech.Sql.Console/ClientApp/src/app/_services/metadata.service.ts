import { Injectable } from '@angular/core';
import {BaseService} from '../_base/base.service';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Response} from '../_models/responses/base/response';

/**
 * Service for retrieving metadata information from the API.
 * This service provides methods to fetch client ID and JWT public key.
 */
@Injectable()
export class MetadataService extends BaseService {

  readonly endpoint: string = '/api/metadata';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  /**
   * Retrieves the client ID from the metadata API.
   * @returns An observable that emits a response containing the client ID.
   */
  getClientId(): Observable<Response<string>> {
    return this.requestGet(`${this.endpoint}/client-id`);
  }

  /**
   * Retrieves the JWT public key from the metadata API.
   * @returns An observable that emits a response containing the JWT public key.
   */
  getJwtPublicKey(): Observable<Response<string>> {
    return this.requestGet(`${this.endpoint}/jwt-public-key`);
  }
}