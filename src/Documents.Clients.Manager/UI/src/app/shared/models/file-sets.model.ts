import { IAllowedOperation, IFileIdentifier, IFileViewer, IMessaging } from '../index';
import { SafeResourceUrl } from '@angular/platform-browser';

// Viewer types sets or categories: returned from specific filesets calls:
export type FileSetTypes = IFileSetBase | IMediaSet | IDocumentSet | IImageSet | ITextSet;
export interface IFileSetBase {
  rootFileIdentifier: IFileIdentifier;
  allowedOperations?: IAllowedOperation[];
  views?: IFileViewer[];
  name?: string;
  message?: IMessaging;
}

// Text Sets: text files
// ------------------------------------------------
export interface ITextSet extends IFileSetBase {
  textType: string; // unknown, text
  fileIdentifier: IFileIdentifier;
}

// Image Sets: image files jpg, jpeg, png, tiff, etc
// ------------------------------------------------
export interface IImageSet extends IFileSetBase {
  imageType: string; // unknown, image
  fileIdentifier: IFileIdentifier;
  PreviewImageIdentifier: IFileIdentifier;
}

// Documents Sets: pdf and docx/doc
// ------------------------------------------------
export interface IDocumentSet extends IFileSetBase {
  documentType: string; // unknown, pdf
  fileIdentifier: IFileIdentifier;
}

// Media Sets: Videos and Audios
// ------------------------------------------------
export interface IMediaSet extends IFileSetBase {
  mediaType?: string; // video, audio unknown
  autoPlay?: boolean;
  preload?: boolean;
  poster?: IFileIdentifier;
  posterSafeUrl?: SafeResourceUrl;
  segments?: IMediaSegment[];
  sources?: IMediaSource[];
  subtitles?: IMediaSubtitles[];
  canonicalURLFormat?: string;
}

export interface IMediaSegment {
  endTime?: number;
  startTime?: number;
  text?: string;
  isActive?: boolean;
  textOriginal?: string;
  startTimeFormatted?: string;
  endTimeFormatted?: string;
  mutes?: IMediaSegment[]; // mutedSegment
}

export interface IMediaSource {
  fileIdentifier: IFileIdentifier;
  type: string; // 'video/webm' | 'video/mp4' | 'video/ogg'
  safeUrl?: SafeResourceUrl;
}

export interface IMediaSubtitles {
  fileIdentifier: IFileIdentifier;
  label: string;
  language: string;
  isDefault: boolean;
  safeUrl?: SafeResourceUrl;
}
