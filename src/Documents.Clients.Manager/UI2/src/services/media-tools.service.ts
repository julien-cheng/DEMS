import * as _ from 'lodash';
import {
  IMediaSource,
  IMediaSubtitles,
  IMediaSegment,
  IMediaSet,
} from '@/interfaces/file-sets.model';
import { IFileIdentifier } from '@/interfaces/identifiers.model';
import { IBatchResponse } from '@/interfaces/request-api';
const { isEqual } = _;

export class MediaToolsService {
  constructor() {}

  // Description: Build the video/audio sources array
  buildMediaSources(sources: any[]): IMediaSource[] {
    let safeSources: IMediaSource[] = [],
      fileUrl;
    sources.forEach(source => {
      fileUrl = this.getFileContentURL(source.fileIdentifier);
      let obj = Object.assign(source, {
        saferesourceURL: fileUrl, // this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl)
      });
      safeSources.push(obj);
    });
    return safeSources;
  }

  // Description: Build the video/audio subtitle/track array
  buildMediaSubtitles(subtitle: any[]): IMediaSubtitles[] {
    let safeSubtitle: IMediaSubtitles[] = [],
      fileUrl;
    subtitle.forEach(subtitle => {
      fileUrl = this.getFileContentURL(subtitle.fileIdentifier);
      let obj = Object.assign(subtitle, {
        saferesourceURL: fileUrl, //this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl)
      });
      safeSubtitle.push(obj);
    });
    return safeSubtitle;
  }

  // Description: Build the image url
  public getFileContentURL(fileIdentifier: IFileIdentifier) {
    return `/api/file/contents?fileidentifier.organizationKey=${fileIdentifier.organizationKey}&fileidentifier.folderKey=${fileIdentifier.folderKey}&fileidentifier.fileKey=${fileIdentifier.fileKey}&open=true`;
  }

  // Description: set the active prop in the segment object
  public setActiveSegment(
    segments: IMediaSegment[],
    activeSegment: IMediaSegment,
  ): IMediaSegment[] {
    return !!activeSegment
      ? segments.map(segment => {
          segment['isActive'] =
            _.isEqual(segment, activeSegment) ||
            (segment.startTime === activeSegment.startTime &&
              segment.endTime === activeSegment.endTime &&
              segment.text === activeSegment.text);
          return segment;
        })
      : segments;
  }

  // Description: Remove all active flags on activesegment undefined or null
  public resetActiveSegment(segments: IMediaSegment[]): IMediaSegment[] {
    return (
      !!segments &&
      segments.filter(segment => {
        segment['isActive'] = false;
        return true;
      })
    );
  }

  // Remove nullables and undefined from mutes objects -> causing bugs
  public defaultNullableMuteSegment(segments: IMediaSegment[]): IMediaSegment[] {
    return !!segments
      ? segments.map(segment => {
          !Array.isArray(segment.mutes) && (segment.mutes = []);
          return segment;
        })
      : segments;
  }

  // Description: Find correct segment by currentTime in seconds
  public getSegmentIndexByStartTime(
    segments: IMediaSegment[],
    currentTime: number,
    isMsec: boolean = false,
  ): number {
    let segmentIndex: number,
      i = 0;
    !isMsec && (currentTime = currentTime * 1000); // Convert to msec if its not assed as such
    !!segments &&
      !Number.isNaN(currentTime) &&
      segments.every((segment: IMediaSegment) => {
        const isActiveSegment = segment.startTime <= currentTime && segment.endTime >= currentTime;
        isActiveSegment && (segmentIndex = i);
        i++;
        return !isActiveSegment;
      });
    return segmentIndex;
  }

  // Description: Find correct segment by startTime and endTIme in ms
  public getSegmentIndexByTime(
    segments: IMediaSegment[],
    startTime: number,
    endTime: number,
  ): number {
    let segmentIndex: number = -1,
      i = 0;
    !!segments &&
      segments.every((segment: IMediaSegment) => {
        const isActiveSegment = segment.startTime === startTime && segment.endTime === endTime;
        isActiveSegment && (segmentIndex = i);
        i++;
        return !isActiveSegment;
      });
    return segmentIndex;
  }

  // Description: Save transcript segments
  public saveMediaSegments(mediaSet: IMediaSet): Observable<IBatchResponse> {
    const headers = new HttpHeaders({ 'Content-type': 'application/json' });
    const options = { headers: headers };
    const url = `/api/views/transcriptset`;
    if (
      !!mediaSet.rootFileIdentifier.organizationKey &&
      !!mediaSet.rootFileIdentifier.folderKey &&
      !!mediaSet.rootFileIdentifier.fileKey
    ) {
      return this.http.put<IBatchResponse>(url, mediaSet, options).catch(this.handleError);
    } else {
      return Observable.throw(new Error('Error in Save Media Segments.'));
    }
  }

  // Media clipping
  // ---------------------------------
  // Description: Add segment in the segments object at correct startTime
  public addNewSegmentByStartTime(
    segments: IMediaSegment[],
    segment: IMediaSegment,
  ): IMediaSegment[] {
    if (!!segments) {
      const start = segment.startTime;
      let index: number = 0;
      segments.length > 0 &&
        segments.forEach((s, i) => {
          // console.log(start, s.startTime,i);
          if (start > s.startTime) {
            index = i + 1;
            return false;
          }
        });
      index <= segments.length ? segments.splice(index, 0, segment) : segments.push(segment);
    }
    return segments;
  }

  // Description: Save clip segments
  saveMediaClipSets(mediaSet: IMediaSet): Observable<IBatchResponse> {
    const headers = new HttpHeaders({ 'Content-type': 'application/json' });
    const options = { headers: headers };
    const url = `/api/views/clipset`;
    if (
      !!mediaSet.rootFileIdentifier.organizationKey &&
      !!mediaSet.rootFileIdentifier.folderKey &&
      !!mediaSet.rootFileIdentifier.fileKey
    ) {
      return this.http.put<IBatchResponse>(url, mediaSet, options).catch(this.handleError);
    } else {
      return Observable.throw(new Error('Error in Save Media Segments.'));
    }
  }

  // Description: return the index of a segment in the object
  public getSegmentIndex(segments: IMediaSegment[], segment: IMediaSegment): number {
    if (!!segments && segment) {
      return segments.findIndex(searchSegment => {
        return (
          _.isEqual(searchSegment, segment) ||
          (searchSegment.startTime === segment.startTime &&
            searchSegment.endTime === segment.endTime &&
            searchSegment.text === segment.text)
        );
      });
    } else {
      return -1;
    }
  }
  // Description: Format time in user friendly
  public formatTime(value: number) {}

  private handleError(error?: Response | string) {
    if (typeof error === 'string') {
      return Observable.throw(error);
    } else {
      return Observable.throw(error.statusText);
    }
  }
}
