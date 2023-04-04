import { Pipe, PipeTransform } from '@angular/core';
import {formatMilliSeconds} from '../utils';
@Pipe({
  name: 'formatMilliseconds'
})
export class FormatMillisecondsPipe implements PipeTransform {

  transform(value: number): string {
    return formatMilliSeconds(value);
  }

}