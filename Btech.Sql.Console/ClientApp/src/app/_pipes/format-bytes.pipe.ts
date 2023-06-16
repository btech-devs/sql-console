import {Pipe, PipeTransform} from '@angular/core';
import {formatBytes} from '../utils';

/**
 * Pipe for formatting a number representing bytes into a human-readable string format.
 * It uses the `formatBytes` utility function to perform the formatting.
 */
@Pipe({
    name: 'formatBytes'
})
export class FormatBytesPipe implements PipeTransform {

    /**
     * Transforms a number representing bytes into a human-readable string format.
     * @param value The number of bytes to format.
     * @returns The formatted string representing the bytes.
     */
    transform(value: number): string {
        return formatBytes(value);
    }

}