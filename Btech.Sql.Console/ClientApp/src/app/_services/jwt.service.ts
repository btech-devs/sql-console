import {Injectable} from '@angular/core';
import {verify, decode} from 'jws';
import {LocalStorageService} from './localStorageService';
import {AlertStorage, JWT_PUBLIC_KEY_KEY} from '../utils';

/**
 * Service for JSON Web Token (JWT) operations.
 * It provides functionality to verify and validate JWT tokens.
 */
@Injectable()
export class JwtService {

    constructor() {
    }

    /**
     * Verifies the signature of a JWT token using the provided public key.
     * @param token The JWT token to verify.
     * @param publicKey The public key used for signature verification.
     * @returns A boolean value indicating whether the signature is valid.
     * @throws An error if the signature verification fails.
     */
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

    /**
     * Verifies the validity of a JWT token by verifying its signature using the stored public key.
     * @param token The JWT token to verify.
     * @returns A boolean value indicating whether the token is valid.
     * @throws An error if the public key is not found or the signature verification fails.
     */
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

    /**
     * Validates a JWT token by verifying its signature and checking its expiration.
     * @param token The JWT token to validate.
     * @returns An object with `isValid` and `isExpired` properties indicating the token's validity and expiration status.
     * @throws An error if the token verification or decoding fails.
     */
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