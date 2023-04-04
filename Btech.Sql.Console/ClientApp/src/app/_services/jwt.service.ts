import {Injectable} from '@angular/core';
import {verify, decode} from 'jws';
import {LocalStorageService} from './localStorageService';
import {AlertStorage, JWT_PUBLIC_KEY_KEY} from '../utils';

@Injectable()
export class JwtService {

    constructor() {
    }

    private verifySignature(token: string, publicKey: string) : boolean {

        let decodedJwt = decode(token);
        let verificationResult : boolean = false;

        try {
            verificationResult = verify(token, decodedJwt.header.alg, publicKey);
        } catch (error) {
            throw new Error('Signature verification failed.');
        }

        if (!verificationResult) {
            throw new Error('Signature verification failed.');
        }

        return true;
    }

    private verifyToken(token: string) : boolean {

        let result : boolean = false;
        let jwtPublicKey : string | null = LocalStorageService.get(JWT_PUBLIC_KEY_KEY);

        if(!jwtPublicKey) {
            throw new Error(`'${JWT_PUBLIC_KEY_KEY}' was not found. Session authentication needed.`);
        }
        else {
            result = this.verifySignature(token, jwtPublicKey);
        }

        return result;
    }

    validateToken(token: string) : { isValid: boolean, isExpired: boolean } {

        let isValid: boolean = true;
        let isExpired: boolean = false;

        try{
            let decodedJwt = decode(token);

            if(this.verifyToken(token)) {
                let exp = decodedJwt.payload.exp;

                if (exp != null) {
                    let date: Date = new Date();
                    let utcNow: number = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds()) / 1000;

                    if (utcNow > exp)
                        isExpired = true;
                }

            } else {
                isValid = false;
            }
        }
        catch(exception: any){
            LocalStorageService.delete(JWT_PUBLIC_KEY_KEY);
            AlertStorage.error = exception.message;
            isValid = false;
        }

        return { isValid, isExpired };
    }


}