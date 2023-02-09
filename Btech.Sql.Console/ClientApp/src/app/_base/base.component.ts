import {Injectable, OnInit} from '@angular/core';

@Injectable()
export abstract class BaseComponent implements OnInit {
    ngOnInit(): void {
    }
}