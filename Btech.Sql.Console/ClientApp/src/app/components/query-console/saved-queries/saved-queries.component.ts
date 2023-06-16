import {
    AfterViewInit,
    Component,
    ElementRef,
    EventEmitter,
    HostListener,
    Output,
    ViewChild
} from '@angular/core';
import {SavedQueriesService} from '../../../_services/saved-queries.service';
import {SavedQuery} from '../../../_models/responses/saved-query/saved-query.model';
import {createPopper, Instance, VirtualElement} from '@popperjs/core';
import * as CodeMirror from 'codemirror';
import {Editor} from 'codemirror';
import 'codemirror/addon/display/autorefresh';
import {FormControl, FormGroup, Validators} from '@angular/forms';
import {AlertStorage} from '../../../utils';
import {map, Observable, Subscription} from 'rxjs';
import {HttpErrorResponse} from '@angular/common/http';

declare var $: any;

type SavedQueryViewModel = SavedQuery & { isLoading: Boolean };

/**
 * Component for managing saved queries.
 * @class
 */
@Component({
    selector: 'app-saved-queries',
    templateUrl: './saved-queries.component.html',
    styleUrls: ['./saved-queries.component.less']
})
export class SavedQueriesComponent implements AfterViewInit {
    constructor(private _savedQueriesService: SavedQueriesService) {
        this._form = new FormGroup<any>(
            {
                name: new FormControl<string>('', [Validators.required, Validators.maxLength(128)]),
                query: new FormControl<string>('', [Validators.required])
            }
        );
    }

    private readonly _form!: FormGroup;

    private _savedQueries?: SavedQueryViewModel[];
    private _popover?: Instance;
    private _selectedQueryIndex?: number;
    private _selectedToEditQueryIndex?: number;
    private _codeEditor?: Editor;
    private _isLoading: boolean = false;
    private _isSupported: boolean = false;

    @ViewChild('popover') popover!: ElementRef<HTMLDivElement>;
    @ViewChild('editor') editor!: ElementRef<HTMLTextAreaElement>;
    @ViewChild('editorEditModal') editorEditModal!: ElementRef<HTMLTextAreaElement>;
    @ViewChild('saveModal') saveModal!: ElementRef<HTMLDivElement>;
    @ViewChild('editModal') editModal!: ElementRef<HTMLDivElement>;
    @Output() insertQuery: EventEmitter<string> = new EventEmitter<string>();
    @Output() onClose: EventEmitter<any> = new EventEmitter<any>();

    /**
     * Gets the saved queries.
     * @returns {SavedQueryViewModel[] | undefined} The saved queries.
     */
    get savedQueries(): SavedQueryViewModel[] | undefined {
        return this._savedQueries;
    }

    /**
     * Gets whether the feature is supported.
     * @returns {boolean} `true` if the feature is supported, `false` otherwise.
     */
    get isSupported(): boolean {
        return this._isSupported;
    }

    /**
     * Gets the form group.
     * @returns {FormGroup} The form group.
     */
    get form(): FormGroup {
        return this._form;
    }

    /**
     * Gets whether the component is in a loading state.
     * @returns {boolean} `true` if the component is loading, `false` otherwise.
     */
    get isLoading(): boolean {
        return this._isLoading;
    }

    /**
     * Gets the selected query.
     * @returns {SavedQueryViewModel | undefined} The selected query.
     */
    get selectedQuery(): SavedQueryViewModel | undefined {

        let savedQueryViewModel: SavedQueryViewModel | undefined;

        if (this._selectedQueryIndex != undefined && this.savedQueries) {
            savedQueryViewModel = this.savedQueries[this._selectedQueryIndex!];
        }

        return savedQueryViewModel;
    }

    /**
     * Gets the selected query to edit.
     * @returns {SavedQueryViewModel | undefined} The selected query to edit.
     */
    get selectedToEditQuery(): SavedQueryViewModel | undefined {

        let savedQueryViewModel: SavedQueryViewModel | undefined = undefined;

        if (this._selectedToEditQueryIndex != undefined && this.savedQueries) {
            savedQueryViewModel = this.savedQueries[this._selectedToEditQueryIndex!];
        }

        return savedQueryViewModel;
    }

    /**
     * Closes the popover and emits the `onClose` event.
     */
    private closePopover(): void {
        this.popover.nativeElement.classList.remove('show');
        this._popover?.destroy();
        this._popover = undefined;
        this._selectedQueryIndex = undefined;
        this.onClose.emit();
    }

    ngAfterViewInit(): void {
        this
            .getAllQueries()
            .add(() => {
                if (this.isSupported) {
                    // After render
                    setTimeout(() => {
                        let resetComponent: () => void = (): void => {
                            (this._codeEditor as any)?.toTextArea();

                            this._form.controls.name.reset('');
                            this._form.markAsUntouched();
                            this._selectedToEditQueryIndex = undefined;
                        };

                        $(this.saveModal.nativeElement).on('hidden.bs.modal', resetComponent);

                        $(this.editModal.nativeElement).on('hidden.bs.modal', resetComponent);
                    }, 10);
                }
            });
    }

    /**
     * Retrieves all queries.
     * @returns {Subscription} The subscription object.
     */
    getAllQueries(): Subscription {
        return this._savedQueriesService
            .getAll()
            .subscribe({
                next: response => {
                    this._isSupported = true;
                    this._savedQueries = <SavedQueryViewModel[] | undefined>response.data;
                },
                error: (error: HttpErrorResponse) => {
                    if (error.status == 501) {
                        this._isSupported = false;
                    }
                }
            });
    }

    /**
     * Retrieves a query by its ID.
     * @param {number} id - The ID of the query to retrieve.
     * @returns {Observable<SavedQuery | undefined>} An observable that emits the query or `undefined`.
     */
    getQuery(id: number): Observable<SavedQuery | undefined> {
        return this._savedQueriesService
            .get(id)
            .pipe(map((response) => {
                if (response?.errorMessage)
                    AlertStorage.error = response.errorMessage;

                return response.data;
            }));
    }

    /**
     * Updates the selected query.
     */
    updateQuery(): void {
        if (this.selectedToEditQuery) {
            this.selectedToEditQuery.name = this.form.value.name;
            this.selectedToEditQuery.query = this._codeEditor!.getValue();
            this.selectedToEditQuery.isLoading = true;

            this._savedQueriesService
                .put(this.selectedToEditQuery)
                .subscribe(response => {
                    if (response?.errorMessage)
                        AlertStorage.error = response.errorMessage;
                })
                .add(() => this.selectedToEditQuery!.isLoading = false);
        }
    }

    /**
     * Resets the edit form to the selected query's values.
     */
    resetEditForm(): void {
        if (this.selectedToEditQuery) {
            this.form.controls.name.reset(this.selectedToEditQuery.name);
            this._codeEditor?.setValue(this.selectedToEditQuery.query!);
            this.form.controls.query.reset(this.selectedToEditQuery.query!);
        }
    }

    /**
     * Adds a new query.
     */
    addQuery(): void {
        this._isLoading = true;

        this._savedQueriesService.post({
            name: this.form.value.name,
            query: this._codeEditor?.getValue()
        })
            .subscribe(response => {
                if (response?.errorMessage) {
                    AlertStorage.error = response.errorMessage;
                } else {
                    AlertStorage.info = `Query: '${this.form.value.name}' has been saved.`;
                    this.closeSaveModal();
                }

            })
            .add(() => {
                this.getAllQueries();
                this._isLoading = false;
            });
    }

    /**
     * Deletes a query.
     * @param {SavedQueryViewModel} query - The query to delete.
     */
    deleteQuery(query: SavedQueryViewModel): void {
        query.isLoading = true;
        this._savedQueriesService.delete(query.id!)
            .subscribe(response => {
                if (!response?.errorMessage) {
                    this._savedQueries = this._savedQueries?.filter(SavedQueryViewModel => query.id != SavedQueryViewModel.id);

                    if (this._selectedToEditQueryIndex == query.id) {
                        this._selectedToEditQueryIndex = undefined;
                    }
                }
            })
            .add(() => query.isLoading = false);
    }

    /**
     * Handles the click event.
     * @param {MouseEvent} event - The click event.
     */
    @HostListener('document:pointerdown', ['$event']) onClick(event: MouseEvent): void {
        if (!event.defaultPrevented &&
            this._popover &&
            !(event.target == this.popover.nativeElement || $(event.target).parents('#select-query-modal').length)) {
            this.closePopover();
        }
    }

    /**
     * Handles the keydown event.
     * @param {KeyboardEvent} event - The keydown event.
     */
    @HostListener('document:keydown', ['$event']) onKeyDown(event: KeyboardEvent): void {
        if (this._popover) {
            if (event.key != 'ArrowDown' && event.key != 'Enter' && event.key != 'ArrowUp' && !(event.key == 'v' && event.altKey))
                this.closePopover();
        }
    }

    /**
     * Shows the list of saved queries.
     * @param {number} left - The left position of the popover.
     * @param {number} top - The top position of the popover.
     * @param {MouseEvent | undefined} event - The click event (optional).
     */
    showPopover(left: number, top: number, event?: MouseEvent): void {
        if (!this._popover) {
            let virtualElement: VirtualElement = {
                getBoundingClientRect: () => {
                    return {
                        width: 0,
                        height: 0,
                        top: top,
                        left: left,
                    } as ClientRect;
                }
            };

            this.popover.nativeElement.focus();

            this.popover.nativeElement.classList.add('show');
            this._popover = createPopper(
                virtualElement,
                this.popover.nativeElement,
                {
                    strategy: 'fixed',
                    placement: 'bottom'
                });
        }

        event?.preventDefault();
    }

    /**
     * Select previous query.
     */
    goUp(): void {
        if (this._selectedQueryIndex == undefined || (this._selectedQueryIndex < 0)) {
            if ((this.savedQueries?.length ?? 0) > 0) {
                this._selectedQueryIndex = this.savedQueries!.length - 1;
            } else
                this._selectedQueryIndex = 0;
        } else {
            this._selectedQueryIndex--;
        }

        this.scrollIntoView();
    }

    /**
     * Select next query.
     */
    goDown(): void {
        if (this._selectedQueryIndex == undefined || (this._selectedQueryIndex > (this._savedQueries?.length ?? 0))) {
            this._selectedQueryIndex = 0;
        } else {
            this._selectedQueryIndex++;
        }

        this.scrollIntoView();
    }

    /**
     * Scroll to selected query.
     */
    scrollIntoView(): void {
        // After render
        setTimeout(() => {
            $('.saved-query.active')[0]?.scrollIntoView();
        }, 1);
    }

    /**
     * Action on enter button has been pressed.
     */
    enter(): void {
        if (this.selectedQuery)
            this.onInsertQuery(this.selectedQuery);
    }

    /**
     * Action on insert query: Lazy loading a query.
     */
    onInsertQuery(query: SavedQueryViewModel): void {
        if (query.query?.length) {
            this.insertQuery.emit(query.query);
            this.closePopover();
        } else {
            query.isLoading = true;
            this.getQuery(query.id!)
                .subscribe(data => {
                    if (data?.query?.length) {
                        query.query = data.query;

                        this.insertQuery.emit(query.query);
                    }
                })
                .add(() => {
                    query.isLoading = false;
                    this.closePopover();
                });
        }
    }

    /**
     * Action triggered when a query is selected for editing.
     *
     * @param index - The index of the selected query.
     */
    onSelectToEdit(index: number): void {
        this._selectedToEditQueryIndex = index;
        this.form.markAsUntouched();

        if (this.selectedToEditQuery) {
            this._isLoading = true;

            this.getQuery(this.selectedToEditQuery.id!)
                .subscribe(data => {
                    if (data) {
                        this._savedQueries![index] = <SavedQueryViewModel>data;
                        this._codeEditor?.setValue(data.query!);
                        this.form.controls.name.reset(data.name);
                        this.form.controls.query.reset(data.query);
                    }


                })
                .add(() => this._isLoading = false);
        }
    }

    /**
     * Displays the save modal with the provided value.
     *
     * @param value - The value to display in the editor.
     */
    showSaveModal(value: string): void {
        this._codeEditor = CodeMirror
            .fromTextArea(this.editor?.nativeElement, {
                autoRefresh: true,
                readOnly: 'nocursor',
                spellcheck: true,
                lineNumbers: true,
                autoCloseBrackets: true
            });

        this._codeEditor.setValue(value);
        this._form.controls.query.setValue(value);
        this._codeEditor.refresh();

        $(this.saveModal.nativeElement).modal('show');
    }

    /**
     * Closes the save modal.
     */
    closeSaveModal(): void {
        $(this.saveModal.nativeElement).modal('hide');
    }

    /**
     * Displays the edit modal with the editor initialized.
     */
    showEditModal(): void {
        if (this.editorEditModal) {
            this._codeEditor = CodeMirror
                .fromTextArea(this.editorEditModal?.nativeElement, {
                    autoRefresh: true,
                    spellcheck: true,
                    lineNumbers: true,
                    autoCloseBrackets: true
                });

            this._codeEditor.on('change', (editor: Editor) => {
                this._form.controls.query.setValue(editor.getValue());
                this._form.controls.query.markAsTouched();
                this._form.controls.query.markAsDirty();
            });
        }

        $(this.editModal.nativeElement).modal('show');
    }
}