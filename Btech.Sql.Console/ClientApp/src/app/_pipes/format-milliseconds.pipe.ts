import { Pipe, PipeTransform } from '@angular/core';
import {formatMilliSeconds} from '../utils';

/**
 * Pipe for formatting a number representing milliseconds into a human-readable string format.
 * It uses the `formatMilliseconds` utility function to perform the formatting.
 */
@Pipe({
  name: 'formatMilliseconds'
})
export class FormatMillisecondsPipe implements PipeTransform {

  /**
   * Transforms a number representing milliseconds into a human-readable string format.
   * @param value The number of milliseconds to format.
   * @returns The formatted string representing the milliseconds.
   */
  transform(value: number): string {
    return formatMilliSeconds(value);
  }

}