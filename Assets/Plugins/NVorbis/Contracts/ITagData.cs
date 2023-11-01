using System.Collections.Generic;

namespace NVorbis.Contracts
{
    /// <summary>
    /// Describes an interface to tag data.
    /// </summary>
    public interface ITagData
    {
        /// <summary>
        /// Gets the full list of tags encountered in the stream.
        /// </summary>
        IReadOnlyDictionary<string, IReadOnlyList<string>> All { get; }

        /// <summary>
        /// The vendor string from the stream header.
        /// </summary>
        string EncoderVendor { get; }

        #region Standard Tags

        /// <summary>
        /// Track/Work name.
        /// </summary>
        string Title { get; }           // TITLE

        /// <summary>
        /// Track version name.
        /// </summary>
        string Version { get; }         // VERSION

        /// <summary>
        /// The collection name to which this track belongs.
        /// </summary>
        string Album { get; }           // ALBUM

        /// <summary>
        /// The track number of this piece if part of a specific larger collection or album.
        /// </summary>
        string TrackNumber { get; }     // TRACKNUMBER

        /// <summary>
        /// The artist generally considered responsible for the work.
        /// </summary>
        string Artist { get; }          // ARTIST

        /// <summary>
        /// The artist(s) who performed the work.
        /// </summary>
        IReadOnlyList<string> Performers { get; }   // PERFORMER; can be "PERFORMER[instrument]"

        /// <summary>
        /// Copyright attribution.
        /// </summary>
        string Copyright { get; }       // COPYRIGHT

        /// <summary>
        /// License information.
        /// </summary>
        string License { get; }         // LICENSE

        /// <summary>
        /// The organization producing the track.
        /// </summary>
        string Organization { get; }    // ORGANIZATION

        /// <summary>
        /// A short text description of the contents.
        /// </summary>
        string Description { get; }     // DESCRIPTION

        /// <summary>
        /// A short text indication of the music genre.
        /// </summary>
        IReadOnlyList<string> Genres { get; }       // GENRE

        /// <summary>
        /// Date the track was recorded.  May have other dates with descriptions.
        /// </summary>
        IReadOnlyList<string> Dates { get; }        // DATE; value is ISO 8601 date with free text following

        /// <summary>
        /// Location where the track was recorded.
        /// </summary>
        IReadOnlyList<string> Locations { get; }    // LOCATION

        /// <summary>
        /// Contact information for the creators or distributors of the track.
        /// </summary>
        string Contact { get; }         // CONTACT

        /// <summary>
        /// The International Standard Recording Code number for the track.
        /// </summary>
        string Isrc { get; }            // ISRC

        #endregion

        #region Extended Tags
        /*
        -- cover art and related...
        COVERART
        COVERARTMIME
        METADATA_BLOCK_PICTURE

        -- exiftool "common" tags
        COMPOSER
        DIRECTOR
        ACTOR
        ENCODED_BY
        ENCODED_USING
        ENCODER
        ENCODER_OPTIONS
        REPLAYGAIN_ALBUM_GAIN
        REPLAYGAIN_ALBUM_PEAK
        REPLAYGAIN_TRACK_GAIN
        REPLAYGAIN_TRACK_PEAK

        -- David Shea tags
        SOURCE ARTIST
        CONDUCTOR
        ENSEMBLE
        REMIXER
        PRODUCER
        ENGINEER
        GUEST ARTIST
        PUBLISHER
        PRODUCTNUMBER
        CATALOGNUMBER
        VOLUME
        RELEASE DATE
        SOURCE MEDIUM

        -- reactor-core.org tags
        ARRANGER
        AUTHOR
        COMMENT
        DISCNUMBER
        EAN/UPN
        ENCODED-BY
        ENCODING
        LABEL
        LABELNO
        LYRICIST
        OPUS
        PART
        PARTNUMBER
        SOURCE WORK
        SOURCEMEDIA
        SPARS
        */
        #endregion

        /// <summary>
        /// Retrieves the value of a tag as a single value.
        /// </summary>
        /// <param name="key">The tag name to retrieve.</param>
        /// <param name="concatenate"><see langword="true"/> to concatenate multiple instances into multiple lines. <see langword="false"/> to return just the last instance.</param>
        /// <returns>The requested tag value, if available.  Otherwise <see langword="null"/>.</returns>
        string GetTagSingle(string key, bool concatenate = false);

        /// <summary>
        /// Retrieves the values of a tag.
        /// </summary>
        /// <param name="key">The tag name to retrieve.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the values in the order read from the stream.</returns>
        IReadOnlyList<string> GetTagMulti(string key);
    }
}