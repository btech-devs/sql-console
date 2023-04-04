import {Pipe, PipeTransform} from '@angular/core';
import {formatBytes} from '../utils';

@Pipe({
    name: 'formatBytes'
})
export class FormatBytesPipe implements PipeTransform {

    transform(value: number): string {
        return formatBytes(value);
    }

}